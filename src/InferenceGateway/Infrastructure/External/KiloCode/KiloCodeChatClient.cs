// <copyright file="KiloCodeChatClient.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.External.KiloCode
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using Microsoft.Extensions.AI;
    using Synaxis.InferenceGateway.Infrastructure;

    /// <summary>
    /// KiloCodeChatClient class.
    /// </summary>
    public sealed class KiloCodeChatClient : IChatClient
    {
        private static readonly Uri KiloApiUri = new("https://api.kilo.ai/api/openrouter");
        private readonly HttpClient? _httpClient;
        private readonly GenericOpenAiChatClient _innerClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="KiloCodeChatClient"/> class.
        /// </summary>
        /// <param name="apiKey">The API key for authentication.</param>
        /// <param name="modelId">The model identifier to use.</param>
        /// <param name="httpClient">Optional HTTP client instance.</param>
        public KiloCodeChatClient(string apiKey, string modelId, HttpClient? httpClient = null)
        {
            this._httpClient = httpClient;
            this._innerClient = new GenericOpenAiChatClient(apiKey, KiloApiUri, modelId, GetKiloHeaders(), httpClient);
        }

        /// <inheritdoc/>
        public ChatClientMetadata Metadata => this._innerClient.Metadata;

        /// <inheritdoc/>
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            return this._innerClient.GetResponseAsync(messages, options, cancellationToken);
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            return this._innerClient.GetStreamingResponseAsync(messages, options, cancellationToken);
        }

        /// <inheritdoc/>
        public object? GetService(Type serviceType, object? serviceKey = null)
        {
            return this._innerClient.GetService(serviceType, serviceKey);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this._innerClient.Dispose();
            this._httpClient?.Dispose();
        }

        private static Dictionary<string, string> GetKiloHeaders()
        {
            return new Dictionary<string, string>
            {
                { "X-KiloCode-EditorName", "Synaxis" },
                { "X-KiloCode-Version", "1.0.0" },
                { "X-KiloCode-TaskId", "synaxis-inference" },
            };
        }
    }
}
