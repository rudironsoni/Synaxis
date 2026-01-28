using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;
using System.Net.Http;
using System.Net.Http.Headers;
using Synaxis.InferenceGateway.Infrastructure.External.GitHub;

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

    public static IServiceCollection AddGitHubCopilotSdk(this IServiceCollection services, string name = "GitHubCopilot")
    {
        // Register the adapter as a singleton - the underlying SDK client may be stateful
        services.AddSingleton<ICopilotSdkAdapter, CopilotSdkAdapter>();

        // Register the CopilotSdkClient as a keyed IChatClient
        services.AddKeyedSingleton<IChatClient>(name, (sp, k) =>
        {
            var adapter = sp.GetRequiredService<ICopilotSdkAdapter>();
            return new CopilotSdkClient(adapter);
        });

        return services;
    }

    public static IServiceCollection AddDuckDuckGo(this IServiceCollection services, string name, string modelId)
    {
        services.AddKeyedSingleton<IChatClient>(name, (sp, k) =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
            return new Synaxis.InferenceGateway.Infrastructure.External.DuckDuckGo.DuckDuckGoChatClient(httpClient, modelId);
        });
        return services;
    }

    public static IServiceCollection AddAiHorde(this IServiceCollection services, string name, string apiKey)
    {
        services.AddKeyedSingleton<IChatClient>(name, (sp, k) =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
            return new Synaxis.InferenceGateway.Infrastructure.External.AiHorde.AiHordeChatClient(httpClient, apiKey);
        });
        return services;
    }
}
