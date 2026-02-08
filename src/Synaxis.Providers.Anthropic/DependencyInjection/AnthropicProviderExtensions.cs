// <copyright file="AnthropicProviderExtensions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.Anthropic.DependencyInjection
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Synaxis.Abstractions.Providers;
    using Synaxis.Providers.Anthropic.Configuration;

    /// <summary>
    /// Dependency injection extensions for Anthropic providers.
    /// </summary>
    public static class AnthropicProviderExtensions
    {
        /// <summary>
        /// Adds Anthropic chat provider to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="apiKey">The Anthropic API key.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddAnthropicChatProvider(
            this IServiceCollection services,
            string apiKey)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException("API key cannot be null or empty.", nameof(apiKey));
            }

            return services.AddAnthropicChatProvider(options =>
            {
                options.ApiKey = apiKey;
            });
        }

        /// <summary>
        /// Adds Anthropic chat provider to the service collection with configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Configuration action for Anthropic options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddAnthropicChatProvider(
            this IServiceCollection services,
            Action<AnthropicOptions> configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            services.Configure(configure);
            services.AddHttpClient<AnthropicClient>();
            services.AddSingleton<AnthropicClient>();
            services.AddSingleton<IChatProvider, AnthropicChatProvider>();

            return services;
        }
    }
}
