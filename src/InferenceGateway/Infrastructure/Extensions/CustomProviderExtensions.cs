using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Synaxis.InferenceGateway.Infrastructure.Extensions;

public static class CustomProviderExtensions
{
    public static IServiceCollection AddCohere(this IServiceCollection services, string apiKey, string modelId)
    {
        services.AddChatClient(sp =>
        {
            var httpClient = new HttpClient();
            return new CohereChatClient(httpClient, modelId, apiKey);
        });
        return services;
    }

    public static IServiceCollection AddPollinations(this IServiceCollection services, string? modelId = null)
    {
        services.AddChatClient(sp =>
        {
            var httpClient = new HttpClient();
            return new PollinationsChatClient(httpClient, modelId);
        });
        return services;
    }

    public static IServiceCollection AddCloudflare(this IServiceCollection services, string apiKey, string accountId, string modelId)
    {
        services.AddChatClient(sp =>
        {
            var httpClient = new HttpClient();
            return new CloudflareChatClient(httpClient, accountId, modelId, apiKey);
        });
        return services;
    }
}
