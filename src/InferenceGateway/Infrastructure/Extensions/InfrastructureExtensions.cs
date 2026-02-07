// <copyright file="InfrastructureExtensions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Extensions
{
    using System.Diagnostics;
    using System.Net.Http;
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.AI;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Polly;
    using Polly.Extensions.Http;
    using Polly.Registry;
    using StackExchange.Redis;
    using Synaxis.InferenceGateway.Application.ChatClients;
    using Synaxis.InferenceGateway.Application.ChatClients.Strategies;
    using Synaxis.InferenceGateway.Application.Configuration;
    using Synaxis.InferenceGateway.Application.ControlPlane;
    using Synaxis.InferenceGateway.Application.Identity;
    using Synaxis.InferenceGateway.Application.Routing;
    using Synaxis.InferenceGateway.Application.Security;
    using Synaxis.InferenceGateway.Infrastructure.Auth;
    using Synaxis.InferenceGateway.Infrastructure.ChatClients;
    using Synaxis.InferenceGateway.Infrastructure.ChatClients.Strategies;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
    using Synaxis.InferenceGateway.Infrastructure.External.GitHub;
    using Synaxis.InferenceGateway.Infrastructure.External.ModelsDev;
    using Synaxis.InferenceGateway.Infrastructure.External.OpenAi;
    using Synaxis.InferenceGateway.Infrastructure.Identity.Core;
    using Synaxis.InferenceGateway.Infrastructure.Identity.Strategies.GitHub;
    using Synaxis.InferenceGateway.Infrastructure.Identity.Strategies.Google;
    using Synaxis.InferenceGateway.Infrastructure.Routing;
    using Synaxis.InferenceGateway.Infrastructure.Security;
    using Synaxis.InferenceGateway.Infrastructure.Services;

    /// <summary>
    /// Extension methods for configuring infrastructure services.
    /// </summary>
    public static class InfrastructureExtensions
    {
        /// <summary>
        /// Adds Synaxis infrastructure services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The application configuration.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddSynaxisInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddTelemetryAndResilience();
            services.AddRedisServices();
            services.AddAuthenticationServices();
            services.AddSecurityServices();
            services.AddRoutingServices();
            services.AddProviderClients(configuration);
            services.AddAntigravityHttpClient();
            services.AddInfrastructureHelpers();
            services.AddSmartRouter();

            return services;
        }

        private static IServiceCollection AddTelemetryAndResilience(this IServiceCollection services)
        {
            services.AddSingleton(new ActivitySource("Synaxis.InferenceGateway"));

            services.AddResiliencePipeline("provider-retry", (builder, context) =>
            {
                builder.AddRetry(new Polly.Retry.RetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    Delay = TimeSpan.FromSeconds(1),
                });
                builder.AddTimeout(TimeSpan.FromMinutes(2));
            });

            return services;
        }

        private static IServiceCollection AddRedisServices(this IServiceCollection services)
        {
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var connectionString = config.GetConnectionString("Redis") ?? "localhost";

                if (!connectionString.Contains("abortConnect=", StringComparison.OrdinalIgnoreCase))
                {
                    connectionString += ",abortConnect=false";
                }

                return ConnectionMultiplexer.Connect(connectionString);
            });

            services.AddSingleton<IHealthStore, RedisHealthStore>();
            services.AddSingleton<IQuotaTracker, RedisQuotaTracker>();

            return services;
        }

        private static IServiceCollection AddAuthenticationServices(this IServiceCollection services)
        {
            services.AddSingleton<AntigravitySettings>(sp =>
            {
                var config = sp.GetRequiredService<IOptions<SynaxisConfiguration>>().Value;
                return config.Antigravity ?? new AntigravitySettings();
            });

            services.AddSingleton<ITokenStore>(sp =>
            {
                var config = sp.GetRequiredService<IOptions<SynaxisConfiguration>>().Value;
                var providerConfig = config.Providers.Values.FirstOrDefault(p => string.Equals(p.Type, "antigravity", StringComparison.OrdinalIgnoreCase));
                var defaultPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".synaxis", "antigravity-auth.json");
                var authPath = providerConfig?.AuthStoragePath ?? defaultPath;
                var logger = sp.GetRequiredService<ILogger<FileTokenStore>>();
                return new FileTokenStore(authPath, logger);
            });

            services.AddSingleton<ISecureTokenStore>(sp =>
            {
                var provider = sp.GetRequiredService<IDataProtectionProvider>();
                var config = sp.GetRequiredService<IOptions<SynaxisConfiguration>>().Value;
                var providerConfig = config.Providers.Values.FirstOrDefault(p => string.Equals(p.Type, "antigravity", StringComparison.OrdinalIgnoreCase));
                var defaultPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".synaxis", "identity-auth.json");
                var authPath = providerConfig?.AuthStoragePath ?? defaultPath;
                return new EncryptedFileTokenStore(provider, authPath);
            });

            services.AddSingleton<IdentityManager>();
            services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<IdentityManager>());
            services.AddSingleton<IAuthStrategy, GitHubAuthStrategy>();
            services.AddSingleton<IAuthStrategy, GoogleAuthStrategy>();
            services.AddSingleton<DeviceFlowService>();

            services.AddSingleton<IAntigravityAuthManager>(sp =>
            {
                var config = sp.GetRequiredService<IOptions<SynaxisConfiguration>>().Value;
                var providerConfig = config.Providers.Values.FirstOrDefault(p => string.Equals(p.Type, "antigravity", StringComparison.OrdinalIgnoreCase));
                var projectId = providerConfig?.ProjectId ?? "rising-fact-p41fc";
                var settings = config.Antigravity ?? new AntigravitySettings();

                return new AntigravityAuthManager(
                    projectId,
                    settings,
                    sp.GetRequiredService<ILogger<AntigravityAuthManager>>(),
                    sp.GetRequiredService<IHttpClientFactory>(),
                    sp.GetRequiredService<ITokenStore>());
            });

            services.AddSingleton<ITokenProvider, Synaxis.InferenceGateway.Infrastructure.Identity.IdentityTokenProvider>();
            services.AddHttpClient("GitHub");
            services.AddHttpClient("Google");
            services.AddHttpClient("Antigravity");

            return services;
        }

        private static IServiceCollection AddSecurityServices(this IServiceCollection services)
        {
            services.AddScoped<ITokenVault, AesGcmTokenVault>();
            services.AddSingleton<IApiKeyService, Security.ApiKeyService>();
            services.AddSingleton<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IAuditService, AuditService>();
            services.AddSingleton<IJwtService, JwtService>();
            services.AddScoped<IIdentityService, Services.IdentityService>();

            return services;
        }

        private static IServiceCollection AddRoutingServices(this IServiceCollection services)
        {
            services.AddScoped<ICostService, CostService>();
            services.AddScoped<IControlPlaneStore, ControlPlaneStore>();

            return services;
        }

        private static IServiceCollection AddProviderClients(this IServiceCollection services, IConfiguration configuration)
        {
            var synaxisConfig = configuration.GetSection("Synaxis:InferenceGateway").Get<SynaxisConfiguration>();
            if (synaxisConfig == null)
            {
                return services;
            }

            foreach (var provider in synaxisConfig.Providers)
            {
                var name = provider.Key;
                var config = provider.Value;
                if (!config.Enabled)
                {
                    continue;
                }

                var defaultModel = config.Models.FirstOrDefault(m => !string.Equals(m, "*", StringComparison.Ordinal)) ?? "default";
                RegisterProviderClient(services, name, config, defaultModel);
            }

            return services;
        }

        private static void RegisterProviderClient(IServiceCollection services, string name, ProviderConfig config, string defaultModel)
        {
            switch (config.Type?.ToLowerInvariant())
            {
                case "openai":
                    RegisterOpenAiClient(services, name, config, defaultModel);
                    break;
                case "groq":
                    RegisterGroqClient(services, name, config, defaultModel);
                    break;
                case "cohere":
                    RegisterCohereClient(services, name, config, defaultModel);
                    break;
                case "cloudflare":
                    RegisterCloudflareClient(services, name, config, defaultModel);
                    break;
                case "gemini":
                    RegisterGeminiClient(services, name, config, defaultModel);
                    break;
                case "antigravity":
                    RegisterAntigravityClient(services, name, config, defaultModel);
                    break;
                case "openrouter":
                    services.AddOpenRouterClient(name, config.Key ?? string.Empty, defaultModel);
                    break;
                case "nvidia":
                    services.AddNvidiaClient(name, config.Key ?? string.Empty, defaultModel);
                    break;
                case "huggingface":
                    services.AddHuggingFaceClient(name, config.Key ?? string.Empty, defaultModel);
                    break;
                case "pollinations":
                    RegisterPollinationsClient(services, name, defaultModel);
                    break;
                case "githubcopilot":
                    RegisterGitHubCopilotClient(services, name, defaultModel);
                    break;
                case "duckduckgo":
                    RegisterDuckDuckGoClient(services, name, defaultModel);
                    break;
                case "aihorde":
                    RegisterAiHordeClient(services, name, config);
                    break;
                case "kilocode":
                    RegisterKiloCodeClient(services, name, config, defaultModel);
                    break;
                default:
                    RegisterDefaultClient(services, name, config, defaultModel);
                    break;
            }
        }

        private static void RegisterOpenAiClient(IServiceCollection services, string name, ProviderConfig config, string defaultModel)
        {
#pragma warning disable S1075 // URIs should not be hardcoded - Default API endpoint
            var headers = config.CustomHeaders != null
                ? new List<KeyValuePair<string, string>>(config.CustomHeaders.Select(kvp => new KeyValuePair<string, string>(kvp.Key, kvp.Value)))
                : null;
            services.AddOpenAiCompatibleClient(name, config.Endpoint ?? "https://api.openai.com/v1", config.Key ?? string.Empty, defaultModel, headers);
#pragma warning restore S1075 // URIs should not be hardcoded
        }

        private static void RegisterGroqClient(IServiceCollection services, string name, ProviderConfig config, string defaultModel)
        {
#pragma warning disable S1075 // URIs should not be hardcoded - Default API endpoint
            var headers = config.CustomHeaders != null
                ? new List<KeyValuePair<string, string>>(config.CustomHeaders.Select(kvp => new KeyValuePair<string, string>(kvp.Key, kvp.Value)))
                : null;
            services.AddOpenAiCompatibleClient(name, "https://api.groq.com/openai/v1", config.Key ?? string.Empty, defaultModel, headers);
#pragma warning restore S1075 // URIs should not be hardcoded
        }

        private static void RegisterCohereClient(IServiceCollection services, string name, ProviderConfig config, string defaultModel)
        {
#pragma warning disable IDISP001 // Dispose created - HttpClient from factory is managed by DI container
            services.AddKeyedSingleton<IChatClient>(name, (sp, k) =>
            {
                var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
                return new CohereChatClient(httpClient, defaultModel, config.Key ?? string.Empty);
            });
#pragma warning restore IDISP001 // Dispose created
        }

        private static void RegisterCloudflareClient(IServiceCollection services, string name, ProviderConfig config, string defaultModel)
        {
#pragma warning disable IDISP001 // Dispose created - HttpClient from factory is managed by DI container
            services.AddKeyedSingleton<IChatClient>(name, (sp, k) =>
            {
                var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
                return new CloudflareChatClient(httpClient, config.AccountId ?? string.Empty, defaultModel, config.Key ?? string.Empty);
            });
#pragma warning restore IDISP001 // Dispose created
        }

        private static void RegisterGeminiClient(IServiceCollection services, string name, ProviderConfig config, string defaultModel)
        {
#pragma warning disable IDISP001 // Dispose created - HttpClient from factory is managed by DI container
            services.AddKeyedSingleton<IChatClient>(name, (sp, k) =>
            {
                var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("Google");
                return new Synaxis.InferenceGateway.Infrastructure.External.Google.GoogleChatClient(config.Key ?? string.Empty, defaultModel, httpClient);
            });
#pragma warning restore IDISP001 // Dispose created
        }

        private static void RegisterPollinationsClient(IServiceCollection services, string name, string defaultModel)
        {
#pragma warning disable IDISP001 // Dispose created - HttpClient from factory is managed by DI container
            services.AddKeyedSingleton<IChatClient>(name, (sp, k) =>
            {
                var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
                return new PollinationsChatClient(httpClient, defaultModel);
            });
#pragma warning restore IDISP001 // Dispose created
        }

        private static void RegisterDefaultClient(IServiceCollection services, string name, ProviderConfig config, string defaultModel)
        {
            if (!string.IsNullOrEmpty(config.Endpoint))
            {
                var headers = config.CustomHeaders != null
                    ? new List<KeyValuePair<string, string>>(config.CustomHeaders.Select(kvp => new KeyValuePair<string, string>(kvp.Key, kvp.Value)))
                    : null;
                services.AddOpenAiCompatibleClient(name, config.Endpoint, config.Key ?? string.Empty, defaultModel, headers);
            }
        }

        private static void RegisterAntigravityClient(IServiceCollection services, string name, ProviderConfig config, string defaultModel)
        {
#pragma warning disable IDISP001 // Dispose created - HttpClient from factory is managed by DI container
            services.AddKeyedSingleton<IChatClient>(name, (sp, k) =>
            {
                var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("Antigravity");
                var authManager = sp.GetRequiredService<IAntigravityAuthManager>();
                return new AntigravityChatClient(httpClient, defaultModel, config.ProjectId ?? string.Empty, authManager);
            });
#pragma warning restore IDISP001 // Dispose created
        }

        private static void RegisterGitHubCopilotClient(IServiceCollection services, string name, string defaultModel)
        {
#pragma warning disable IDISP001 // Dispose created - HttpClient from factory is managed by DI container
            services.AddKeyedSingleton<IChatClient>(name, (sp, k) =>
            {
                var adapter = sp.GetService<ICopilotSdkAdapter>();
                if (adapter != null)
                {
                    return new CopilotSdkClient(adapter);
                }

                var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
                return new Synaxis.InferenceGateway.Infrastructure.External.DuckDuckGo.DuckDuckGoChatClient(httpClient, defaultModel);
            });
#pragma warning restore IDISP001 // Dispose created
        }

        private static void RegisterDuckDuckGoClient(IServiceCollection services, string name, string defaultModel)
        {
#pragma warning disable IDISP001 // Dispose created - HttpClient from factory is managed by DI container
            services.AddKeyedSingleton<IChatClient>(name, (sp, k) =>
            {
                var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
                return new Synaxis.InferenceGateway.Infrastructure.External.DuckDuckGo.DuckDuckGoChatClient(httpClient, defaultModel);
            });
#pragma warning restore IDISP001 // Dispose created
        }

        private static void RegisterAiHordeClient(IServiceCollection services, string name, ProviderConfig config)
        {
#pragma warning disable IDISP001 // Dispose created - HttpClient from factory is managed by DI container
            services.AddKeyedSingleton<IChatClient>(name, (sp, k) =>
            {
                var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
                return new Synaxis.InferenceGateway.Infrastructure.External.AiHorde.AiHordeChatClient(httpClient, config.Key ?? "0000000000");
            });
#pragma warning restore IDISP001 // Dispose created
        }

        private static void RegisterKiloCodeClient(IServiceCollection services, string name, ProviderConfig config, string defaultModel)
        {
#pragma warning disable IDISP001 // Dispose created - HttpClient from factory is managed by DI container
            services.AddKeyedSingleton<IChatClient>(name, (sp, k) =>
            {
                var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
                return new Synaxis.InferenceGateway.Infrastructure.External.KiloCode.KiloCodeChatClient(config.Key ?? string.Empty, defaultModel, httpClient);
            });
#pragma warning restore IDISP001 // Dispose created
        }

        private static IServiceCollection AddAntigravityHttpClient(this IServiceCollection services)
        {
            services.AddHttpClient("Antigravity", client =>
            {
#pragma warning disable S1075 // URIs should not be hardcoded - Default API endpoint
                client.BaseAddress = new Uri("https://cloudcode-pa.googleapis.com");
#pragma warning restore S1075 // URIs should not be hardcoded
            })
            .AddPolicyHandler(GetRetryPolicy());

            return services;
        }

        private static IServiceCollection AddSmartRouter(this IServiceCollection services)
        {
#pragma warning disable IDISP001 // Dispose created - ChatClientBuilder manages disposal
            services.AddScoped<IChatClient>(sp =>
            {
                var innerClient = ActivatorUtilities.CreateInstance<SmartRoutingChatClient>(sp);

                var builder = new ChatClientBuilder(innerClient);
                builder.UseFunctionInvocation();
                builder.Use((inner, services) => new UsageTrackingChatClient(inner, services.GetRequiredService<ILogger<UsageTrackingChatClient>>()));

                return builder.Build(sp);
            });
#pragma warning restore IDISP001 // Dispose created

            return services;
        }

        // Register infrastructure-level helpers
        private static IServiceCollection AddInfrastructureHelpers(this IServiceCollection services)
        {
            services.AddScoped<IChatClientFactory, ChatClientFactory>();
            services.AddSingleton<IChatClientStrategy, OpenAiGenericStrategy>();
            services.AddSingleton<IChatClientStrategy, CloudflareStrategy>();

            // Register Models.dev client
#pragma warning disable S1075 // URIs should not be hardcoded - Default API endpoint
            services.AddHttpClient<IModelsDevClient, ModelsDevClient>(client =>
            {
                client.BaseAddress = new Uri("https://models.dev");
            });
#pragma warning restore S1075 // URIs should not be hardcoded

            // Register OpenAI model discovery client
            services.AddHttpClient<IOpenAiModelDiscoveryClient, OpenAiModelDiscoveryClient>();
            return services;
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }

        /// <summary>
        /// Initializes the database by running migrations asynchronously.
        /// </summary>
        /// <param name="host">The application host.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task InitializeDatabaseAsync(this Microsoft.Extensions.Hosting.IHost host)
        {
            using var scope = host.Services.CreateScope();
            var services = scope.ServiceProvider;

            try
            {
                var configuration = services.GetRequiredService<IConfiguration>();

                // Default to true when the configuration key is missing
                var runMigrations = configuration.GetValue<bool>("Synaxis:ControlPlane:RunMigrations", true);
                if (!runMigrations)
                {
                    var logger = services.GetRequiredService<ILogger<ControlPlaneDbContext>>();
                    logger.LogWarning("Skipping database migration per configuration.");
                    return;
                }

                var context = services.GetRequiredService<ControlPlaneDbContext>();
                await context.Database.MigrateAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<ControlPlaneDbContext>>();
                logger.LogError(ex, "An error occurred while migrating the database.");
                throw new InvalidOperationException("Database migration failed. See inner exception for details.", ex);
            }
        }
    }
}
