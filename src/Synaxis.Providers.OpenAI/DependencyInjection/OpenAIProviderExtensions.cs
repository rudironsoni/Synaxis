// <copyright file="OpenAIProviderExtensions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.OpenAI.DependencyInjection
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Synaxis.Abstractions.Providers;
    using Synaxis.Providers.OpenAI.Configuration;

    /// <summary>
    /// Extension methods for registering OpenAI provider services.
    /// </summary>
    public static class OpenAIProviderExtensions
    {
        /// <summary>
        /// Adds OpenAI provider services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="apiKey">The OpenAI API key.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddOpenAIProvider(
            this IServiceCollection services,
            string apiKey)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

            return services.AddOpenAIProvider(options =>
            {
                options.ApiKey = apiKey;
            });
        }

        /// <summary>
        /// Adds OpenAI provider services to the service collection with configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">The configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddOpenAIProvider(
            this IServiceCollection services,
            Action<OpenAIOptions> configureOptions)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configureOptions);

            services.Configure(configureOptions);

            services.AddHttpClient<OpenAIClient>();

            services.AddSingleton<IChatProvider, OpenAIChatProvider>();
            services.AddSingleton<IEmbeddingProvider, OpenAIEmbeddingProvider>();
            services.AddSingleton<IImageProvider, OpenAIImageProvider>();
            services.AddSingleton<IAudioProvider, OpenAIAudioProvider>();

            return services;
        }
    }
}
