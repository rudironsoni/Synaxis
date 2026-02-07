// <copyright file="AiHordeChatClient.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.External.AiHorde
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Json;
    using System.Runtime.CompilerServices;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.AI;

    /// <summary>
    /// Chat client implementation for AI Horde service.
    /// </summary>
    public sealed class AiHordeChatClient : IChatClient
    {
#pragma warning disable S1075 // URIs should not be hardcoded - API endpoints
        private const string GenerateUrl = "https://stablehorde.net/api/v2/generate/text/async";
        private const string StatusUrlTemplate = "https://stablehorde.net/api/v2/generate/text/status/{0}";
#pragma warning restore S1075 // URIs should not be hardcoded

#pragma warning disable IDISP008 // Don't assign member with injected and created disposables - ownership tracked by _ownsHttpClient flag
        private readonly HttpClient _httpClient;
#pragma warning restore IDISP008
        private readonly bool _ownsHttpClient;
        private readonly ChatClientMetadata _metadata;
        private readonly string _apiKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="AiHordeChatClient"/> class.
        /// </summary>
        /// <param name="httpClient">Optional HTTP client instance.</param>
        /// <param name="apiKey">The API key for authentication.</param>
        public AiHordeChatClient(HttpClient? httpClient = null, string apiKey = "0000000000")
        {
            if (httpClient is null)
            {
                this._httpClient = new HttpClient();
                this._ownsHttpClient = true;
            }
            else
            {
                this._httpClient = httpClient;
                this._ownsHttpClient = false;
            }

            this._apiKey = apiKey;
            this._metadata = new ChatClientMetadata("AiHorde", new Uri(GenerateUrl), "aihorde");
        }

        /// <summary>
        /// Gets the metadata for this chat client.
        /// </summary>
        public ChatClientMetadata Metadata => this._metadata;

        /// <inheritdoc/>
        public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            var prompt = BuildPrompt(messages);

            var request = new { prompt = prompt, models = new[] { "stable" } };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, GenerateUrl);
            httpRequest.Headers.TryAddWithoutValidation("apikey", this._apiKey);
            httpRequest.Content = JsonContent.Create(request);

            using var response = await this._httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var genResp = await response.Content.ReadFromJsonAsync<GenerateResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
            if (genResp == null || string.IsNullOrEmpty(genResp.Id))
            {
                return new ChatResponse(new List<ChatMessage>());
            }

            var id = genResp.Id;

            // Poll status until done
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(200, cancellationToken).ConfigureAwait(false);
                using var statusReq = new HttpRequestMessage(HttpMethod.Get, string.Format(StatusUrlTemplate, id));
                statusReq.Headers.TryAddWithoutValidation("apikey", this._apiKey);
                using var statusResp = await this._httpClient.SendAsync(statusReq, cancellationToken).ConfigureAwait(false);
                statusResp.EnsureSuccessStatusCode();

                var status = await statusResp.Content.ReadFromJsonAsync<StatusResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
                if (status == null)
                {
                    break;
                }

                if (status.Done)
                {
                    var text = status?.Text ?? string.Empty;
                    var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, text));
                    chatResponse.ResponseId = id;
                    return chatResponse;
                }
            }

            return new ChatResponse(new List<ChatMessage>());
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // We'll implement polling and yield final result when ready
            var prompt = BuildPrompt(messages);
            var request = new { prompt = prompt, models = new[] { "stable" } };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, GenerateUrl);
            httpRequest.Headers.TryAddWithoutValidation("apikey", this._apiKey);
            httpRequest.Content = JsonContent.Create(request);

            using var response = await this._httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var genResp = await response.Content.ReadFromJsonAsync<GenerateResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
            if (genResp == null || string.IsNullOrEmpty(genResp.Id))
            {
                yield break;
            }

            var id = genResp.Id;

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(200, cancellationToken).ConfigureAwait(false);
                using var statusReq = new HttpRequestMessage(HttpMethod.Get, string.Format(StatusUrlTemplate, id));
                statusReq.Headers.TryAddWithoutValidation("apikey", this._apiKey);
                using var statusResp = await this._httpClient.SendAsync(statusReq, cancellationToken).ConfigureAwait(false);
                statusResp.EnsureSuccessStatusCode();
                var status = await statusResp.Content.ReadFromJsonAsync<StatusResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
                if (status == null)
                {
                    yield break;
                }

                if (status.Done)
                {
                    var text = status.Text ?? string.Empty;
                    var update = new ChatResponseUpdate
                    {
                        Role = ChatRole.Assistant,
                    };
                    update.Contents.Add(new TextContent(text));
                    yield return update;
                    yield break;
                }
            }
        }

        private static string BuildPrompt(IEnumerable<ChatMessage> messages)
        {
            var parts = new List<string>();
            foreach (var m in messages)
            {
                parts.Add($"[{m.Role.Value}] {m.Text}");
            }

            return string.Join("\n", parts);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this._ownsHttpClient)
            {
                this._httpClient.Dispose();
            }
        }

        /// <inheritdoc/>
        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        private sealed class GenerateResponse
        {
            /// <summary>
            /// Gets or sets the Id.
            /// </summary>
            [JsonPropertyName("id")]
            public string? Id { get; set; }
        }

        private sealed class StatusResponse
        {
            /// <summary>
            /// Gets or sets a value indicating whether the request is done.
            /// </summary>
            [JsonPropertyName("done")]
            public bool Done { get; set; }

            /// <summary>
            /// Gets or sets the Text.
            /// </summary>
            [JsonPropertyName("text")]
            public string? Text { get; set; }
        }
    }
}
