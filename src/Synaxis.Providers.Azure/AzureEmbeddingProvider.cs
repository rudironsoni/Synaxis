// <copyright file="AzureEmbeddingProvider.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Synaxis.Abstractions.Providers;

    /// <summary>
    /// Azure OpenAI implementation of <see cref="IEmbeddingProvider"/>.
    /// </summary>
    public sealed class AzureEmbeddingProvider : IEmbeddingProvider
    {
        private readonly AzureClient client;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureEmbeddingProvider"/> class.
        /// </summary>
        /// <param name="client">The Azure client for making API requests.</param>
        public AzureEmbeddingProvider(AzureClient client)
        {
            this.client = client!;
        }

        /// <inheritdoc/>
        public string ProviderName => "Azure OpenAI";

        /// <inheritdoc/>
        public Task<object> EmbedAsync(
            IEnumerable<string> inputs,
            string model,
            object? options = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(inputs);
            if (string.IsNullOrWhiteSpace(model))
            {
                throw new ArgumentException("Model cannot be null or whitespace.", nameof(model));
            }

            var requestBody = new
            {
                input = inputs,
                model = model,

                // Merge additional options if provided
            };

            return this.client.PostAsync("embeddings", requestBody, cancellationToken);
        }
    }
}
