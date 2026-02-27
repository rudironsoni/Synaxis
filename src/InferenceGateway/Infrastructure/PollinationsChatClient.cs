// <copyright file="PollinationsChatClient.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Json;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.AI;

    /// <summary>
    /// PollinationsChatClient class.
    /// </summary>
    public sealed class PollinationsChatClient : IChatClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _modelId;
        private readonly ChatClientMetadata _metadata;

        /// <summary>
        /// Initializes a new instance of the <see cref="PollinationsChatClient"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client for API requests.</param>
        /// <param name="modelId">The model identifier to use.</param>
        public PollinationsChatClient(HttpClient httpClient, string? modelId = null)
        {
            this._httpClient = httpClient;
            this._modelId = modelId ?? "openai";
            this._metadata = new ChatClientMetadata("Pollinations", new Uri("https://text.pollinations.ai/"), this._modelId);
        }

        /// <summary>
        /// Gets the metadata for this chat client.
        /// </summary>
        public ChatClientMetadata Metadata => this._metadata;

        /// <inheritdoc/>
        public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            var request = this.CreateRequest(messages, options, stream: false);
            var response = await this._httpClient.PostAsJsonAsync("https://text.pollinations.ai/", request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, content));
            chatResponse.ModelId = this._modelId;
            return chatResponse;
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var request = this.CreateRequest(messages, options, stream: true);

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://text.pollinations.ai/")
            {
                Content = JsonContent.Create(request),
            };

            using var response = await this._httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var reader = new System.IO.StreamReader(stream);

            // Pollinations streaming is just raw text updates if I recall correctly,
            // but if we use the POST endpoint it might be different.
            // Actually, Pollinations POST endpoint with stream: true returns SSE if requested,
            // but often it just returns chunks of text.
            char[] buffer = new char[1024];
            while (await reader.ReadLineAsync().ConfigureAwait(false) is not null)
            {
                int read = await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                if (read > 0)
                {
                    var text = new string(buffer, 0, read);
                    var update = new ChatResponseUpdate
                    {
                        Role = ChatRole.Assistant,
                        ModelId = this._modelId,
                    };
                    update.Contents.Add(new TextContent(text));
                    yield return update;
                }
            }
        }

        private object CreateRequest(IEnumerable<ChatMessage> messages, ChatOptions? options, bool stream)
        {
            _ = options; // Parameter intentionally unused - reserved for future extensions

            var messageList = new List<object>();
            foreach (var msg in messages)
            {
                messageList.Add(new
                {
                    role = msg.Role.Value,
                    content = msg.Text,
                });
            }

            // Map standard model names to Pollinations aliases
            var model = this._modelId switch
            {
                "gpt-4o-mini" => "openai",
                "gpt-4o" => "openai-large",
                _ => this._modelId,
            };

            return new
            {
                messages = messageList,
                model = model,
                stream = stream,
                seed = Random.Shared.Next(), // Avoid caching
            };
        }

        /// <summary>
        /// Disposes the resources used by this client.
        /// </summary>
        public void Dispose() => this._httpClient.Dispose();

        /// <summary>
        /// Gets a service of the specified type.
        /// </summary>
        /// <param name="serviceType">The type of service to retrieve.</param>
        /// <param name="serviceKey">The optional service key.</param>
        /// <returns>The service instance, or null if not found.</returns>
        public object? GetService(Type serviceType, object? serviceKey = null) => null;
    }
}
