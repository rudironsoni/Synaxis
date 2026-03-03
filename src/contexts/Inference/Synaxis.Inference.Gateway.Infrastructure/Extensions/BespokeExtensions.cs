// <copyright file="BespokeExtensions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Extensions
{
    using System;
    using Microsoft.Extensions.AI;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Extension methods for registering bespoke chat client services.
    /// </summary>
    public static class BespokeExtensions
    {
        /// <summary>
        /// Adds a keyed Cohere chat client to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="key">The service key for keyed registration.</param>
        /// <param name="apiKey">The Cohere API key.</param>
        /// <param name="modelId">The model identifier.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddCohereClient(this IServiceCollection services, string key, string apiKey, string modelId)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddHttpClient();
            services.AddKeyedSingleton<IChatClient>(key, (sp, obj) =>
                ActivatorUtilities.CreateInstance<CohereChatClient>(sp, modelId, apiKey));
            return services;
        }

        /// <summary>
        /// Adds a keyed Pollinations chat client to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="key">The service key for keyed registration.</param>
        /// <param name="modelId">The model identifier.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddPollinationsClient(this IServiceCollection services, string key, string modelId)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddHttpClient();
            services.AddKeyedSingleton<IChatClient>(key, (sp, obj) =>
                modelId == null
                    ? ActivatorUtilities.CreateInstance<PollinationsChatClient>(sp)
                    : ActivatorUtilities.CreateInstance<PollinationsChatClient>(sp, modelId));
            return services;
        }
    }
}
