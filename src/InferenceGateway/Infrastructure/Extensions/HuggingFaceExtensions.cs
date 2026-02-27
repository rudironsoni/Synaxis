// <copyright file="HuggingFaceExtensions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Extensions
{
    using Microsoft.Extensions.AI;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// HuggingFaceExtensions class.
    /// </summary>
    public static class HuggingFaceExtensions
    {
        /// <summary>
        /// Adds a HuggingFace chat client to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="serviceKey">The service key for the client.</param>
        /// <param name="apiKey">The API key for authentication.</param>
        /// <param name="modelId">The model identifier.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddHuggingFaceClient(this IServiceCollection services, string serviceKey, string apiKey, string modelId)
        {
            services.AddKeyedSingleton<IChatClient>(serviceKey, (_, _) => new GenericOpenAiChatClient(
                apiKey,
                new Uri("https://router.huggingface.co/v1/"),
                modelId));

            return services;
        }
    }
}
