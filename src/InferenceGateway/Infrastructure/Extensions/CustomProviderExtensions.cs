// <copyright file="CustomProviderExtensions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Extensions
{
    using System.Net.Http;
    using System.Net.Http.Headers;
    using GitHub.Copilot.SDK;
    using Microsoft.Extensions.AI;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Synaxis.InferenceGateway.Infrastructure.External.GitHub;

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
            // Register underlying CopilotClient via adapter reflection as singleton
            services.AddSingleton<ICopilotSdkAdapter, CopilotSdkAdapter>();

            // Register ICopilotClient by wrapping the concrete CopilotClient from the adapter (if available)
            services.AddSingleton<ICopilotClient>(sp =>
            {
                var adapter = sp.GetRequiredService<ICopilotSdkAdapter>();
                var clientObj = adapter.GetService(Type.GetType("GitHub.Copilot.Sdk.CopilotClient, GitHub.Copilot.Sdk") ?? typeof(object));
                var concrete = clientObj as global::GitHub.Copilot.SDK.CopilotClient;
                if (concrete == null) throw new InvalidOperationException("CopilotClient not available");
                return new CopilotClientAdapter(concrete);
            });

            // Register GitHubCopilotChatClient as keyed scoped IChatClient
            services.AddKeyedScoped<IChatClient>(name, (sp, k) =>
            {
                var client = sp.GetRequiredService<ICopilotClient>();
                var logger = sp.GetService<ILogger<GitHubCopilotChatClient>>();
                return new GitHubCopilotChatClient(client, logger);
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
}