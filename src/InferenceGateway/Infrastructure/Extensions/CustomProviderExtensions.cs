using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;
using System.Net.Http;
using System.Net.Http.Headers;
using Synaxis.InferenceGateway.Infrastructure.External.GitHub;
using GitHub.Copilot.SDK;
using Synaxis.InferenceGateway.Infrastructure.External.MicrosoftAgents.GithubCopilot;
using Microsoft.Extensions.Logging;

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

        // Register the GithubCopilotAgent as transient. If the underlying CopilotClient
        // isn't available the agent factory will throw when resolved.
        services.AddTransient<GithubCopilotAgent>(sp =>
        {
            var adapter = sp.GetRequiredService<ICopilotSdkAdapter>();
            var client = adapter.GetService(Type.GetType("GitHub.Copilot.Sdk.CopilotClient, GitHub.Copilot.Sdk") ?? typeof(object)) as CopilotClient;
            return new GithubCopilotAgent(client ?? throw new InvalidOperationException("CopilotClient not available"), sessionConfig: null, ownsClient: false);
        });

        // Register the GithubCopilotAgentClient as a keyed IChatClient
        services.AddKeyedSingleton<IChatClient>(name, (sp, k) =>
        {
            var agent = sp.GetRequiredService<GithubCopilotAgent>();
            var logger = sp.GetService<ILogger<GithubCopilotAgentClient>>();
            return new GithubCopilotAgentClient(agent, logger);
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
