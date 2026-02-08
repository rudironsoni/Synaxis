// <copyright file="OpenAIEmbeddingProvider.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.OpenAI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Synaxis.Abstractions.Providers;
    using Synaxis.Contracts.V1.Messages;
    using Synaxis.Providers.OpenAI.Models;

    /// <summary>
    /// OpenAI implementation of <see cref="IEmbeddingProvider"/>.
    /// </summary>
    public sealed class OpenAIEmbeddingProvider : IEmbeddingProvider
    {
        private readonly OpenAIClient _client;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenAIEmbeddingProvider"/> class.
        /// </summary>
        /// <param name="client">The OpenAI client.</param>
        /// <param name="logger">The logger.</param>
        public OpenAIEmbeddingProvider(
            OpenAIClient client,
            ILogger<OpenAIEmbeddingProvider> logger)
        {
            this._client = client ?? throw new ArgumentNullException(nameof(client));
            _ = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public string ProviderName => "OpenAI";

        /// <inheritdoc/>
        public async Task<object> EmbedAsync(
            IEnumerable<string> inputs,
            string model,
            object? options = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(inputs);
            ArgumentNullException.ThrowIfNull(model);

            var inputList = inputs.ToList();
            if (inputList.Count == 0)
            {
                throw new ArgumentException("At least one input is required", nameof(inputs));
            }

            var request = new OpenAIEmbeddingRequest
            {
                Model = model,
                Input = inputList,
            };

            var response = await this._client.PostAsync<OpenAIEmbeddingRequest, OpenAIEmbeddingResponse>(
                "embeddings",
                request,
                cancellationToken).ConfigureAwait(false);

            return this.MapToSynaxisResponse(response);
        }

        private EmbeddingResponse MapToSynaxisResponse(OpenAIEmbeddingResponse response)
        {
            var data = response.Data
                .Select(d => new EmbeddingData
                {
                    Index = d.Index,
                    Embedding = d.Embedding,
                    Object = d.Object,
                })
                .ToArray();

            EmbeddingUsage? usage = null;
            if (response.Usage is not null)
            {
                usage = new EmbeddingUsage
                {
                    PromptTokens = response.Usage.PromptTokens,
                    TotalTokens = response.Usage.TotalTokens,
                };
            }

            return new EmbeddingResponse
            {
                Object = response.Object,
                Data = data,
                Usage = usage,
            };
        }
    }
}
