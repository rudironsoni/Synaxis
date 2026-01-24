using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Synaxis.Infrastructure.Extensions;

public static class HuggingFaceExtensions
{
    public static IServiceCollection AddHuggingFaceClient(this IServiceCollection services, string serviceKey, string apiKey, string modelId)
    {
        services.AddKeyedSingleton<IChatClient>(serviceKey, (_, _) => new GenericOpenAiChatClient(
            apiKey,
            new Uri("https://api-inference.huggingface.co/v1/"),
            modelId));

        return services;
    }
}
