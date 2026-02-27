// <copyright file="CloudflareChatClient.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Http.Json;
    using System.Runtime.CompilerServices;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.AI;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// CloudflareChatClient class.
    /// </summary>
    public sealed class CloudflareChatClient : IChatClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _accountId;
        private readonly string _modelId;
        private readonly ChatClientMetadata _metadata;
        private readonly ILogger<CloudflareChatClient>? _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudflareChatClient"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client for API requests.</param>
        /// <param name="accountId">The Cloudflare account ID.</param>
        /// <param name="modelId">The model identifier.</param>
        /// <param name="apiKey">The API key for authentication.</param>
        /// <param name="logger">The optional logger instance.</param>
        public CloudflareChatClient(HttpClient httpClient, string accountId, string modelId, string apiKey, ILogger<CloudflareChatClient>? logger = null)
        {
            this._httpClient = httpClient;
            this._accountId = accountId;
            this._modelId = modelId;
            this._logger = logger;
            this._httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            // Store the raw model id; Cloudflare expects path-like model ids (e.g. @cf/meta/...) as raw segments.
            this._metadata = new ChatClientMetadata("Cloudflare", new Uri($"https://api.cloudflare.com/client/v4/accounts/{accountId}/ai/run/{modelId}"), modelId);
        }

        /// <summary>
        /// Gets the metadata for this chat client.
        /// </summary>
        public ChatClientMetadata Metadata => this._metadata;

        /// <inheritdoc/>
        public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            var request = this.CreateRequest(messages, stream: false);
            var url = $"https://api.cloudflare.com/client/v4/accounts/{this._accountId}/ai/run/{this._modelId}";

            // Debug: print the full request URL and model id to help diagnose 404s
            var requestUrl = url;
            if (this._logger != null)
            {
                this._logger.LogInformation("CloudflareChatClient sending request. Url: {Url} ModelId: {ModelId}", requestUrl, this._modelId);
            }
            else
            {
                Console.WriteLine($"CloudflareChatClient sending request. Url: {requestUrl} ModelId: {this._modelId}");
            }

            var response = await this._httpClient.PostAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var cloudflareResponse = await response.Content.ReadFromJsonAsync<CloudflareResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);

            var text = cloudflareResponse?.Result?.Response ?? string.Empty;
            var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, text));
            chatResponse.ModelId = this._modelId;
            return chatResponse;
        }

        /// <summary>
        /// Gets streaming response asynchronously.
        /// </summary>
        /// <param name="messages">The chat messages.</param>
        /// <param name="options">The chat options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An async enumerable of chat response updates.</returns>
        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var request = this.CreateRequest(messages, stream: true);
            var url = $"https://api.cloudflare.com/client/v4/accounts/{this._accountId}/ai/run/{this._modelId}";

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(request),
            };

            using var response = await this._httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var reader = new System.IO.StreamReader(stream);

            string? line;
            while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) is not null)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (!line.StartsWith("data: ", StringComparison.Ordinal))
                {
                    continue;
                }

                var json = line.Substring(6).Trim();
                if (string.Equals(json, "[DONE]", StringComparison.Ordinal))
                {
                    break;
                }

                CloudflareStreamResponse? streamEvent = null;
                try
                {
                    streamEvent = JsonSerializer.Deserialize<CloudflareStreamResponse>(json);
                }
                catch
                {
                    continue;
                }

                if (streamEvent?.Response != null)
                {
                    var update = new ChatResponseUpdate
                    {
                        Role = ChatRole.Assistant,
                        ModelId = this._modelId,
                    };
                    update.Contents.Add(new TextContent(streamEvent.Response));
                    yield return update;
                }
            }
        }

        private object CreateRequest(IEnumerable<ChatMessage> chatMessages, bool stream)
        {
            var messages = new List<object>();
            foreach (var msg in chatMessages)
            {
                messages.Add(new
                {
                    role = msg.Role.Value,
                    content = msg.Text,
                });
            }

            return new
            {
                messages = messages,
                stream = stream,
            };
        }

        /// <summary>
        /// Disposes resources. HttpClient is not disposed as it is injected.
        /// </summary>
        public void Dispose()
        {
            // HttpClient is injected and managed externally, so we don't dispose it here
            // This implementation satisfies the IDisposable contract without disposing injected dependencies
        }

        /// <summary>
        /// Gets a service of the specified type.
        /// </summary>
        /// <param name="serviceType">The type of service to get.</param>
        /// <param name="serviceKey">The optional service key.</param>
        /// <returns>The service instance or null.</returns>
        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        private sealed class CloudflareResponse
        {
            /// <summary>
            /// Gets or sets the Result.
            /// </summary>
            [JsonPropertyName("result")]
            public CloudflareResult? Result { get; set; }
        }

        private sealed class CloudflareResult
        {
            /// <summary>
            /// Gets or sets the Response.
            /// </summary>
            [JsonPropertyName("response")]
            public string? Response { get; set; }
        }

        private sealed class CloudflareStreamResponse
        {
            /// <summary>
            /// Gets or sets the Response.
            /// </summary>
            [JsonPropertyName("response")]
            public string? Response { get; set; }
        }
    }
}
