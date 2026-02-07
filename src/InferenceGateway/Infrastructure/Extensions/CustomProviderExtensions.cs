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

    /// <summary>
    /// Extension methods for registering custom provider chat clients.
    /// </summary>
    public static class CustomProviderExtensions
    {
        /// <summary>
        /// Adds Cohere chat client to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="apiKey">The Cohere API key.</param>
        /// <param name="modelId">The model identifier.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddCohere(this IServiceCollection services, string apiKey, string modelId)
        {
#pragma warning disable IDISP001 // Dispose created - HttpClient from factory is managed by DI container
            services.AddChatClient(sp =>
            {
                var httpClient = new HttpClient();
                return new CohereChatClient(httpClient, modelId, apiKey);
            });
#pragma warning restore IDISP001 // Dispose created
            return services;
        }

        /// <summary>
        /// Adds Pollinations chat client to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="modelId">Optional model identifier.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddPollinations(this IServiceCollection services, string? modelId = null)
        {
#pragma warning disable IDISP001 // Dispose created - HttpClient from factory is managed by DI container
            services.AddChatClient(sp =>
            {
                var httpClient = new HttpClient();
                return new PollinationsChatClient(httpClient, modelId);
            });
#pragma warning restore IDISP001 // Dispose created
            return services;
        }

        /// <summary>
        /// Adds Cloudflare chat client to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="apiKey">The Cloudflare API key.</param>
        /// <param name="accountId">The Cloudflare account identifier.</param>
        /// <param name="modelId">The model identifier.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddCloudflare(this IServiceCollection services, string apiKey, string accountId, string modelId)
        {
#pragma warning disable IDISP001 // Dispose created - HttpClient from factory is managed by DI container
            services.AddChatClient(sp =>
            {
                var httpClient = new HttpClient();
                return new CloudflareChatClient(httpClient, accountId, modelId, apiKey);
            });
#pragma warning restore IDISP001 // Dispose created
            return services;
        }

        /// <summary>
        /// Adds GitHub Copilot SDK chat client to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The keyed service name for registration.</param>
        /// <returns>The service collection for chaining.</returns>
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
                if (concrete == null)
                {
                    throw new InvalidOperationException("CopilotClient not available");
                }

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

        /// <summary>
        /// Adds DuckDuckGo chat client to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The keyed service name for registration.</param>
        /// <param name="modelId">The model identifier.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddDuckDuckGo(this IServiceCollection services, string name, string modelId)
        {
#pragma warning disable IDISP001 // Dispose created - HttpClient from factory is managed by DI container
            services.AddKeyedSingleton<IChatClient>(name, (sp, k) =>
            {
                var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
                return new Synaxis.InferenceGateway.Infrastructure.External.DuckDuckGo.DuckDuckGoChatClient(httpClient, modelId);
            });
#pragma warning restore IDISP001 // Dispose created
            return services;
        }

        /// <summary>
        /// Adds AI Horde chat client to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The keyed service name for registration.</param>
        /// <param name="apiKey">The AI Horde API key.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddAiHorde(this IServiceCollection services, string name, string apiKey)
        {
#pragma warning disable IDISP001 // Dispose created - HttpClient from factory is managed by DI container
            services.AddKeyedSingleton<IChatClient>(name, (sp, k) =>
            {
                var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
                return new Synaxis.InferenceGateway.Infrastructure.External.AiHorde.AiHordeChatClient(httpClient, apiKey);
            });
#pragma warning restore IDISP001 // Dispose created
            return services;
        }
    }
}
