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
using System.Linq;

namespace Synaxis.InferenceGateway.Application.Extensions;

public static class ApplicationExtensions
{
    public static IServiceCollection AddSynaxisApplication(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Register Configuration
        services.Configure<SynaxisConfiguration>(configuration.GetSection("Synaxis"));

        // 2. Register Registry
        services.AddSingleton<IProviderRegistry, ProviderRegistry>();
        services.AddSingleton<IModelResolver, ModelResolver>();

        // 3. Register Providers as Keyed Services
        var synaxisConfig = configuration.GetSection("Synaxis").Get<SynaxisConfiguration>();
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

        // 4. Register the Tiered Router as the primary IChatClient
        services.AddChatClient(sp => ActivatorUtilities.CreateInstance<TieredRoutingChatClient>(sp))
                .UseFunctionInvocation()
                .Use((inner, sp) => new UsageTrackingChatClient(inner, sp.GetRequiredService<ILogger<UsageTrackingChatClient>>()));

        return services;
    }
}
