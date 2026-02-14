// <copyright file="ProviderBuilder.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Synaxis.Providers.Configuration;

    /// <summary>
    /// Builder for configuring provider adapters.
    /// </summary>
    public sealed class ProviderBuilder
    {
        private readonly IServiceCollection _services;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProviderBuilder"/> class.
        /// </summary>
        /// <param name="services">The service collection.</param>
        internal ProviderBuilder(IServiceCollection services)
        {
            this._services = services;
        }

        /// <summary>
        /// Adds the OpenAI provider adapter.
        /// </summary>
        /// <param name="configureOptions">An action to configure the OpenAI options.</param>
        /// <returns>The builder for chaining.</returns>
        public ProviderBuilder AddOpenAI(Action<OpenAIOptions>? configureOptions = null)
        {
            this._services.AddOpenAIAdapter(configureOptions);
            return this;
        }

        /// <summary>
        /// Adds the Azure OpenAI provider adapter.
        /// </summary>
        /// <param name="configureOptions">An action to configure the Azure OpenAI options.</param>
        /// <returns>The builder for chaining.</returns>
        public ProviderBuilder AddAzureOpenAI(Action<AzureOpenAIOptions>? configureOptions = null)
        {
            this._services.AddAzureOpenAIAdapter(configureOptions);
            return this;
        }
    }
}
