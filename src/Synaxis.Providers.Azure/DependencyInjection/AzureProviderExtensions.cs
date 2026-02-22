// <copyright file="AzureProviderExtensions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.Azure.DependencyInjection
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Synaxis.Abstractions.Providers;
    using Synaxis.Providers.Azure.Configuration;

    /// <summary>
    /// Dependency injection extensions for Azure OpenAI providers.
    /// </summary>
    public static class AzureProviderExtensions
    {
        /// <summary>
        /// Adds Azure OpenAI chat provider to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Configuration action for Azure OpenAI options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddAzureOpenAIChatProvider(
            this IServiceCollection services,
            Action<AzureOpenAIOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configure);
            services.Configure(configure);
            services.AddHttpClient<AzureClient>();
            services.AddSingleton<IChatProvider, AzureChatProvider>();

            return services;
        }

        /// <summary>
        /// Adds Azure OpenAI embedding provider to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Configuration action for Azure OpenAI options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddAzureOpenAIEmbeddingProvider(
            this IServiceCollection services,
            Action<AzureOpenAIOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configure);
            services.Configure(configure);
            services.AddHttpClient<AzureClient>();
            services.AddSingleton<IEmbeddingProvider, AzureEmbeddingProvider>();

            return services;
        }

        /// <summary>
        /// Adds both Azure OpenAI chat and embedding providers to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Configuration action for Azure OpenAI options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddAzureOpenAIProviders(
            this IServiceCollection services,
            Action<AzureOpenAIOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configure);
            services.Configure(configure);
            services.AddHttpClient<AzureClient>();
            services.AddSingleton<IChatProvider, AzureChatProvider>();
            services.AddSingleton<IEmbeddingProvider, AzureEmbeddingProvider>();

            return services;
        }
    }
}
