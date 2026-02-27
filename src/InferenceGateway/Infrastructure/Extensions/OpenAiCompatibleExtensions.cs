// <copyright file="OpenAiCompatibleExtensions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using Microsoft.Extensions.AI;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// OpenAiCompatibleExtensions class.
    /// </summary>
    public static class OpenAiCompatibleExtensions
    {
        /// <summary>
        /// Adds an OpenAI-compatible chat client to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="key">The service key for the client.</param>
        /// <param name="baseUrl">The base URL of the API.</param>
        /// <param name="apiKey">The API key for authentication.</param>
        /// <param name="modelId">Optional model identifier.</param>
        /// <param name="customHeaders">Optional custom headers to include in requests.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddOpenAiCompatibleClient(
            this IServiceCollection services,
            string key,
            string baseUrl,
            string apiKey,
            string? modelId = null,
            IList<KeyValuePair<string, string>>? customHeaders = null)
        {
            services.AddKeyedSingleton<IChatClient>(key, (sp, obj) =>
            {
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient(key);
                var headers = customHeaders != null
                    ? new Dictionary<string, string>(customHeaders.Select(kvp => new KeyValuePair<string, string>(kvp.Key, kvp.Value)))
                    : null;
                return new GenericOpenAiChatClient(apiKey, new Uri(baseUrl), modelId ?? "default", headers, httpClient);
            });

            return services;
        }
    }
}
