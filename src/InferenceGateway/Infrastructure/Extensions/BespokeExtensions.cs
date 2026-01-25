using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;

namespace Synaxis.InferenceGateway.Infrastructure.Extensions;

public static class BespokeExtensions
{
    public static IServiceCollection AddCohereClient(this IServiceCollection services, string key, string apiKey, string modelId)
    {
        services.AddKeyedSingleton<IChatClient>(key, (sp, obj) =>
        {
            var httpClient = new HttpClient();
            return new CohereChatClient(httpClient, modelId, apiKey);
        });
        return services;
    }

    public static IServiceCollection AddPollinationsClient(this IServiceCollection services, string key, string modelId)
    {
        services.AddKeyedSingleton<IChatClient>(key, (sp, obj) =>
        {
            var httpClient = new HttpClient();
            return new PollinationsChatClient(httpClient, modelId);
        });
        return services;
    }
}
