// <copyright file="ServiceCollectionExtensions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Synaxis.Providers.Configuration;

    /// <summary>
    /// Extension methods for configuring provider services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the OpenAI provider adapter to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">An action to configure the OpenAI options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddOpenAIAdapter(
            this IServiceCollection services,
            Action<OpenAIOptions>? configureOptions = null)
        {
            if (configureOptions is not null)
            {
                services.Configure(configureOptions);
            }

            services.AddHttpClient<Adapters.OpenAIAdapter>();
            services.AddSingleton<Adapters.OpenAIAdapter>();

            return services;
        }

        /// <summary>
        /// Adds the Azure OpenAI provider adapter to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">An action to configure the Azure OpenAI options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddAzureOpenAIAdapter(
            this IServiceCollection services,
            Action<AzureOpenAIOptions>? configureOptions = null)
        {
            if (configureOptions is not null)
            {
                services.Configure(configureOptions);
            }

            services.AddHttpClient<Adapters.AzureOpenAIAdapter>();
            services.AddSingleton<Adapters.AzureOpenAIAdapter>();

            return services;
        }

        /// <summary>
        /// Adds the provider factory and all configured adapters to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureProviders">An action to configure provider adapters.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddProviders(
            this IServiceCollection services,
            Action<ProviderBuilder>? configureProviders = null)
        {
            var builder = new ProviderBuilder(services);
            configureProviders?.Invoke(builder);

            services.AddSingleton<ProviderFactory>();

            return services;
        }
    }
}
