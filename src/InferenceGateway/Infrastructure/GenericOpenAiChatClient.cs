// <copyright file="GenericOpenAiChatClient.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure
{
    using System;
    using System.ClientModel;
    using System.ClientModel.Primitives;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.AI;
    using OpenAI;

    /// <summary>
    /// A generic OpenAI chat client that wraps the OpenAI SDK and supports custom endpoints and headers.
    /// </summary>
#pragma warning disable IDISP025 // Class with no virtual dispose method should be sealed
#pragma warning disable S3881 // "IDisposable" should be implemented correctly
    public class GenericOpenAiChatClient : IChatClient
#pragma warning restore S3881 // "IDisposable" should be implemented correctly
#pragma warning restore IDISP025 // Class with no virtual dispose method should be sealed
    {
        private readonly IChatClient _innerClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericOpenAiChatClient"/> class.
        /// </summary>
        /// <param name="apiKey">The API key for authentication.</param>
        /// <param name="endpoint">The endpoint URI for the OpenAI service.</param>
        /// <param name="modelId">The model identifier to use.</param>
        /// <param name="customHeaders">Optional custom headers to include in requests.</param>
        /// <param name="httpClient">Optional HttpClient instance to use for requests.</param>
        public GenericOpenAiChatClient(string apiKey, Uri endpoint, string modelId, IDictionary<string, string>? customHeaders = null, HttpClient? httpClient = null)
        {
            var options = new OpenAIClientOptions
            {
                Endpoint = endpoint,
            };

            if (httpClient != null)
            {
                options.Transport = new HttpClientPipelineTransport(httpClient);
            }

            if (customHeaders != null && customHeaders.Count > 0)
            {
                options.AddPolicy(new CustomHeaderPolicy(customHeaders), PipelinePosition.PerCall);
            }

            var openAiClient = new OpenAIClient(new ApiKeyCredential(apiKey), options);
            this._innerClient = openAiClient.GetChatClient(modelId).AsIChatClient();
        }

        /// <summary>
        /// Gets the metadata for this chat client.
        /// </summary>
        public ChatClientMetadata Metadata => this._innerClient.GetService<ChatClientMetadata>() ?? new ChatClientMetadata("OpenAI");

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
        }

        private sealed class CustomHeaderPolicy : PipelinePolicy
        {
            private readonly IDictionary<string, string> _headers;

            public CustomHeaderPolicy(IDictionary<string, string> headers)
            {
                this._headers = headers;
            }

            public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
            {
                foreach (var header in this._headers)
                {
                    message.Request.Headers.Set(header.Key, header.Value);
                }

                ProcessNext(message, pipeline, currentIndex);
            }

            public override ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
            {
                foreach (var header in this._headers)
                {
                    message.Request.Headers.Set(header.Key, header.Value);
                }

                return ProcessNextAsync(message, pipeline, currentIndex);
            }
        }
    }
}
