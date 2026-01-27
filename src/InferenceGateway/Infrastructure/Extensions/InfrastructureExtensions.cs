using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Synaxis.InferenceGateway.Application.Configuration;
using Synaxis.InferenceGateway.Infrastructure.Auth;
using Synaxis.InferenceGateway.Infrastructure.Security;
using Synaxis.InferenceGateway.Application.Security;
using Polly;
using Polly.Extensions.Http;
using Microsoft.Extensions.AI;
using System.Net.Http;
using System.Diagnostics;
using Polly.Registry;

using Synaxis.InferenceGateway.Infrastructure.Routing;
using Synaxis.InferenceGateway.Application.Routing;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
using Synaxis.InferenceGateway.Application.ControlPlane;
using Synaxis.InferenceGateway.Application.ChatClients;
using Synaxis.InferenceGateway.Infrastructure.ChatClients;
using Synaxis.InferenceGateway.Infrastructure.ChatClients.Strategies;
using Synaxis.InferenceGateway.Application.ChatClients.Strategies;
using StackExchange.Redis;
using Microsoft.EntityFrameworkCore;

namespace Synaxis.InferenceGateway.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddSynaxisInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 0. Register Telemetry
        services.AddSingleton(new ActivitySource("Synaxis.InferenceGateway"));

        // 0.1 Register Resilience
        services.AddResiliencePipeline("provider-retry", (builder, context) =>
        {
            builder.AddRetry(new Polly.Retry.RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromSeconds(1)
            });
            builder.AddTimeout(TimeSpan.FromMinutes(2));
        });

        // 0.2 Register Redis
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var connectionString = config.GetConnectionString("Redis") ?? "localhost";

            // Ensure we don't crash if Redis is down (fail-open)
            if (!connectionString.Contains("abortConnect=", StringComparison.OrdinalIgnoreCase))
            {
                connectionString += ",abortConnect=false";
            }

            return ConnectionMultiplexer.Connect(connectionString);
        });

        services.AddSingleton<IHealthStore, RedisHealthStore>();
        services.AddSingleton<IQuotaTracker, RedisQuotaTracker>();

        // 1. Register Auth Manager (Singleton) with Factory
        services.AddSingleton<IAntigravityAuthManager>(sp =>
        {
            var config = sp.GetRequiredService<IOptions<SynaxisConfiguration>>().Value;
            var logger = sp.GetRequiredService<ILogger<AntigravityAuthManager>>();

            // Find Antigravity config
            var providerConfig = config.Providers.Values.FirstOrDefault(p => p.Type?.ToLowerInvariant() == "antigravity");
            var projectId = providerConfig?.ProjectId ?? string.Empty;

            // Determine storage path
            var defaultPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".synaxis", "antigravity-auth.json");
            var authPath = providerConfig?.AuthStoragePath ?? defaultPath;

            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            return new AntigravityAuthManager(projectId, authPath, logger, httpClientFactory);
        });
        services.AddSingleton<ITokenProvider>(sp => sp.GetRequiredService<IAntigravityAuthManager>());

        // 1.5 Register Security Services
        services.AddScoped<ITokenVault, AesGcmTokenVault>();
        services.AddSingleton<IApiKeyService, ApiKeyService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddSingleton<IJwtService, JwtService>();

        // 1.6 Register Routing Services
        services.AddScoped<ICostService, CostService>();
        services.AddScoped<IControlPlaneStore, ControlPlaneStore>();

        // 2. Register Providers as Keyed Services
        var synaxisConfig = configuration.GetSection("Synaxis:InferenceGateway").Get<SynaxisConfiguration>();
        if (synaxisConfig != null)
        {
            foreach (var provider in synaxisConfig.Providers)
            {
                var name = provider.Key;
                var config = provider.Value;
                if (!config.Enabled) continue;
                var defaultModel = config.Models.FirstOrDefault(m => m != "*") ?? "default";

                switch (config.Type?.ToLowerInvariant())
                {
                    case "openai":
                        services.AddOpenAiCompatibleClient(name, config.Endpoint ?? "https://api.openai.com/v1", config.Key ?? "", defaultModel);
                        break;
                    case "groq":
                        services.AddOpenAiCompatibleClient(name, "https://api.groq.com/openai/v1", config.Key ?? "", defaultModel);
                        break;
                    case "cohere":
                        services.AddKeyedSingleton<IChatClient>(name, (sp, k) =>
                        {
                            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
                            return new CohereChatClient(httpClient, defaultModel, config.Key ?? "");
                        });
                        break;
                    case "cloudflare":
                        services.AddKeyedSingleton<IChatClient>(name, (sp, k) =>
                        {
                            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
                            return new CloudflareChatClient(httpClient, config.AccountId ?? "", defaultModel, config.Key ?? "");
                        });
                        break;
                    case "gemini":
                        services.AddKeyedSingleton<IChatClient>(name, (sp, k) =>
                        {
                            var client = new Google.GenAI.Client(vertexAI: false, apiKey: config.Key ?? "");
                            return client.AsIChatClient(defaultModel);
                        });
                        break;
                    case "antigravity":
                        services.AddKeyedSingleton<IChatClient>(name, (sp, k) =>
                        {
                            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("Antigravity");
                            var tokenProvider = sp.GetRequiredService<ITokenProvider>();
                            return new AntigravityChatClient(httpClient, defaultModel, config.ProjectId ?? "", tokenProvider);
                        });
                        break;
                    case "openrouter":
                        services.AddOpenRouterClient(name, config.Key ?? "", defaultModel);
                        break;
                    case "nvidia":
                        services.AddNvidiaClient(name, config.Key ?? "", defaultModel);
                        break;
                    case "huggingface":
                        services.AddHuggingFaceClient(name, config.Key ?? "", defaultModel);
                        break;
                    case "pollinations":
                        services.AddKeyedSingleton<IChatClient>(name, (sp, k) =>
                        {
                            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
                            return new PollinationsChatClient(httpClient, defaultModel);
                        });
                        break;
                    default:
                        if (!string.IsNullOrEmpty(config.Endpoint))
                        {
                            services.AddOpenAiCompatibleClient(name, config.Endpoint, config.Key ?? "", defaultModel);
                        }
                        break;
                }
            }
        }

        // 3. Configure HttpClient for Antigravity with Polly
        services.AddHttpClient("Antigravity")
            .AddPolicyHandler(GetRetryPolicy());

        // Register infrastructure helpers
        services.AddInfrastructureHelpers();

        // 4. Register the Smart Router as the primary IChatClient (Scoped)
        // We manually build the pipeline to ensure it is registered as Scoped,
        // because SmartRoutingChatClient depends on Scoped services (IModelResolver, ICostService).
        services.AddScoped<IChatClient>(sp =>
        {
            var innerClient = ActivatorUtilities.CreateInstance<SmartRoutingChatClient>(sp);

            var builder = new ChatClientBuilder(innerClient);
            builder.UseFunctionInvocation();
            builder.Use((inner, services) => new UsageTrackingChatClient(inner, services.GetRequiredService<ILogger<UsageTrackingChatClient>>()));

            return builder.Build(sp);
        });

        return services;
    }

    // Register infrastructure-level helpers
    private static IServiceCollection AddInfrastructureHelpers(this IServiceCollection services)
    {
        services.AddSingleton<IChatClientFactory, ChatClientFactory>();
        services.AddSingleton<IChatClientStrategy, OpenAiGenericStrategy>();
        services.AddSingleton<IChatClientStrategy, CloudflareStrategy>();
        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    public static async Task InitializeDatabaseAsync(this Microsoft.Extensions.Hosting.IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var context = services.GetRequiredService<ControlPlaneDbContext>();
            await context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<ControlPlaneDbContext>>();
            logger.LogError(ex, "An error occurred while migrating the database.");
            throw;
        }
    }
}
