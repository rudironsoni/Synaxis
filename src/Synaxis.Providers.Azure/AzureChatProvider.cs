// <copyright file="AzureChatProvider.cs" company="Synaxis">
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
    /// Azure OpenAI implementation of <see cref="IChatProvider"/>.
    /// </summary>
    public sealed class AzureChatProvider : IChatProvider
    {
        private readonly AzureClient client;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureChatProvider"/> class.
        /// </summary>
        /// <param name="client">The Azure client for making API requests.</param>
        public AzureChatProvider(AzureClient client)
        {
            this.client = client!;
        }

        /// <inheritdoc/>
        public string ProviderName => "Azure OpenAI";

        /// <inheritdoc/>
        public Task<object> ChatAsync(
            IEnumerable<object> messages,
            string model,
            object? options = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(messages);
            if (string.IsNullOrWhiteSpace(model))
            {
                throw new ArgumentException("Model cannot be null or whitespace.", nameof(model));
            }

            var requestBody = new
            {
                messages = messages,
                model = model,

                // Merge additional options if provided
            };

            return this.client.PostAsync("chat/completions", requestBody, cancellationToken);
        }
    }
}
