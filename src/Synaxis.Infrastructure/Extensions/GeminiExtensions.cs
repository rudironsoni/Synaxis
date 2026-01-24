using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Synaxis.Infrastructure.Extensions;

public static class GeminiExtensions
{
    public static IServiceCollection AddGeminiClient(this IServiceCollection services, string apiKey, string modelId)
    {
        var client = new Google.GenAI.Client(vertexAI: false, apiKey: apiKey);
        
        services.AddChatClient(_ => client.AsIChatClient(modelId));

        return services;
    }
}
