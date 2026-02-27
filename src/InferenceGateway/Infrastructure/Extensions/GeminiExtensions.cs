// <copyright file="GeminiExtensions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Extensions
{
    using Microsoft.Extensions.AI;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// GeminiExtensions class.
    /// </summary>
    public static class GeminiExtensions
    {
        /// <summary>
        /// Adds a Google Gemini chat client to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="apiKey">The API key for authentication.</param>
        /// <param name="modelId">The model identifier.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddGeminiClient(this IServiceCollection services, string apiKey, string modelId)
        {
            var client = new Google.GenAI.Client(vertexAI: false, apiKey: apiKey);

            services.AddChatClient(_ => client.AsIChatClient(modelId));

            return services;
        }
    }
}
