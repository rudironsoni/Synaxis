// <copyright file="ProviderFactory.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers
{
    using System;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Factory for creating provider adapters based on provider type.
    /// </summary>
    public sealed class ProviderFactory
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProviderFactory"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        public ProviderFactory(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Creates a provider adapter for the specified provider type.
        /// </summary>
        /// <param name="providerType">The type of provider.</param>
        /// <returns>The provider adapter.</returns>
        /// <exception cref="ArgumentException">Thrown when the provider type is not supported.</exception>
        public IProviderAdapter CreateAdapter(ProviderType providerType)
        {
            return providerType switch
            {
                ProviderType.OpenAI => this._serviceProvider.GetRequiredService<Adapters.OpenAIAdapter>(),
                ProviderType.AzureOpenAI => this._serviceProvider.GetRequiredService<Adapters.AzureOpenAIAdapter>(),
                _ => throw new ArgumentException($"Unsupported provider type: {providerType}", nameof(providerType)),
            };
        }

        /// <summary>
        /// Creates a provider adapter for the specified provider type.
        /// </summary>
        /// <param name="providerType">The type of provider as a string.</param>
        /// <returns>The provider adapter.</returns>
        /// <exception cref="ArgumentException">Thrown when the provider type is not supported or cannot be parsed.</exception>
        public IProviderAdapter CreateAdapter(string providerType)
        {
            if (Enum.TryParse<ProviderType>(providerType, ignoreCase: true, out var parsedType))
            {
                return this.CreateAdapter(parsedType);
            }

            throw new ArgumentException($"Unsupported provider type: {providerType}", nameof(providerType));
        }
    }
}
