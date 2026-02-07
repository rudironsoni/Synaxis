// <copyright file="OpenRouterExtensions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Extensions
{
    using Microsoft.Extensions.AI;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// OpenRouterExtensions class.
    /// </summary>
    public static class OpenRouterExtensions
    {
        /// <summary>
        /// Adds an OpenRouter chat client to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="serviceKey">The service key for the client.</param>
        /// <param name="apiKey">The API key for authentication.</param>
        /// <param name="modelId">The model identifier.</param>
        /// <param name="siteUrl">Optional site URL for referer header.</param>
        /// <param name="siteName">Optional site name for X-Title header.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddOpenRouterClient(this IServiceCollection services, string serviceKey, string apiKey, string modelId = "auto", string? siteUrl = null, string? siteName = null)
        {
            var headers = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(siteUrl))
            {
                headers.Add("HTTP-Referer", siteUrl);
            }

            if (!string.IsNullOrEmpty(siteName))
            {
                headers.Add("X-Title", siteName);
            }

#pragma warning disable S1075 // URIs should not be hardcoded - API endpoint
            services.AddKeyedSingleton<IChatClient>(serviceKey, (_, _) => new GenericOpenAiChatClient(
                apiKey,
                new Uri("https://openrouter.ai/api/v1/"),
                modelId,
                headers));
#pragma warning restore S1075 // URIs should not be hardcoded

            return services;
        }
    }
}
