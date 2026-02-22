// <copyright file="OpenAIImageProvider.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.OpenAI
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Synaxis.Abstractions.Providers;
    using Synaxis.Contracts.V1.Messages;
    using Synaxis.Providers.OpenAI.Models;

    /// <summary>
    /// OpenAI implementation of <see cref="IImageProvider"/> using DALL-E.
    /// </summary>
    public sealed class OpenAIImageProvider : IImageProvider
    {
        private readonly OpenAIClient _client;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenAIImageProvider"/> class.
        /// </summary>
        /// <param name="client">The OpenAI client.</param>
        /// <param name="logger">The logger.</param>
        public OpenAIImageProvider(
            OpenAIClient client,
            ILogger<OpenAIImageProvider> logger)
        {
            this._client = client!;
            _ = logger!;
        }

        /// <inheritdoc/>
        public string ProviderName => "OpenAI";

        /// <inheritdoc/>
        public async Task<object> GenerateImageAsync(
            string prompt,
            string model,
            object? options = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(prompt);
            ArgumentNullException.ThrowIfNull(model);

            var request = new OpenAIImageRequest
            {
                Prompt = prompt,
                Model = model,
                N = 1,
                Size = "1024x1024",
            };

            var response = await this._client.PostAsync<OpenAIImageRequest, OpenAIImageResponse>(
                "images/generations",
                request,
                cancellationToken).ConfigureAwait(false);

            return this.MapToSynaxisResponse(response);
        }

        private ImageGenerationResponse MapToSynaxisResponse(OpenAIImageResponse response)
        {
            var data = response.Data
                .Select(d => new ImageData
                {
                    Url = d.Url,
                    B64Json = d.B64Json,
                    RevisedPrompt = d.RevisedPrompt,
                })
                .ToArray();

            return new ImageGenerationResponse
            {
                Created = response.Created,
                Data = data,
            };
        }
    }
}
