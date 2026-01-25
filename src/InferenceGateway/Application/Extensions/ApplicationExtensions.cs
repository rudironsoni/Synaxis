using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Synaxis.InferenceGateway.Application.ChatClients;
using Synaxis.InferenceGateway.Application.Configuration;
using Synaxis.InferenceGateway.Application.Routing;
using Synaxis.InferenceGateway.Infrastructure;
using Synaxis.InferenceGateway.Infrastructure.Extensions;
using Synaxis.InferenceGateway.Infrastructure.Auth;
using System.Linq;
using System;
using System.Net.Http;
using Polly;
using Polly.Extensions.Http;

namespace Synaxis.InferenceGateway.Application.Extensions;

public static class ApplicationExtensions
{
    public static IServiceCollection AddSynaxisApplication(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Register Configuration
        services.Configure<SynaxisConfiguration>(configuration.GetSection("Synaxis:InferenceGateway"));

        // 2. Register Registry
        services.AddSingleton<IProviderRegistry, ProviderRegistry>();
        services.AddSingleton<IModelResolver, ModelResolver>();

        // 3. Register Auth Manager (Singleton) with Factory
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

        // 4. Register Providers as Keyed Services
        var synaxisConfig = configuration.GetSection("Synaxis:InferenceGateway").Get<SynaxisConfiguration>();
        if (synaxisConfig != null)
        {
            foreach (var provider in synaxisConfig.Providers)
            {
                var name = provider.Key;
                var config = provider.Value;
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

        // 5. Configure HttpClient for Antigravity with Polly
        services.AddHttpClient("Antigravity")
            .AddPolicyHandler(GetRetryPolicy());

        // 6. Register the Tiered Router as the primary IChatClient
        services.AddChatClient(sp => ActivatorUtilities.CreateInstance<TieredRoutingChatClient>(sp))
                .UseFunctionInvocation()
                .Use((inner, sp) => new UsageTrackingChatClient(inner, sp.GetRequiredService<ILogger<UsageTrackingChatClient>>()));

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }
}
