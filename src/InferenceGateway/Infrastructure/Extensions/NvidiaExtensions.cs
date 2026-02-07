// <copyright file="NvidiaExtensions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Extensions
{
    using Microsoft.Extensions.AI;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// NvidiaExtensions class.
    /// </summary>
    public static class NvidiaExtensions
    {
        /// <summary>
        /// Adds an NVIDIA chat client to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="serviceKey">The service key for the client.</param>
        /// <param name="apiKey">The API key for authentication.</param>
        /// <param name="modelId">The model identifier.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddNvidiaClient(this IServiceCollection services, string serviceKey, string apiKey, string modelId)
        {
#pragma warning disable S1075 // URIs should not be hardcoded - API endpoint
            services.AddKeyedSingleton<IChatClient>(serviceKey, (_, _) => new GenericOpenAiChatClient(
                apiKey,
                new Uri("https://integrate.api.nvidia.com/v1"),
                modelId));
#pragma warning restore S1075 // URIs should not be hardcoded

            return services;
        }
    }
}
