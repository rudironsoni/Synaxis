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

    public class CloudflareChatClient : IChatClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _accountId;
        private readonly string _modelId;
        private readonly ChatClientMetadata _metadata;
        private readonly ILogger<CloudflareChatClient>? _logger;

        public CloudflareChatClient(HttpClient httpClient, string accountId, string modelId, string apiKey, ILogger<CloudflareChatClient>? logger = null)
        {
            this._httpClient = httpClient;
            this._accountId = accountId;
            this._modelId = modelId;
            this._logger = logger;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            // Store the raw model id; Cloudflare expects path-like model ids (e.g. @cf/meta/...) as raw segments.
            this._metadata = new ChatClientMetadata("Cloudflare", new Uri($"https://api.cloudflare.com/client/v4/accounts/{accountId}/ai/run/{modelId}"), modelId);
        }

        public ChatClientMetadata Metadata => this._metadata;

        public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            var request = CreateRequest(chatMessages, options, stream: false);
            var url = $"https://api.cloudflare.com/client/v4/accounts/{_accountId}/ai/run/{_modelId}";

            // Debug: print the full request URL and model id to help diagnose 404s
            var requestUrl = url;
            if (_logger != null)
            {
                _logger.LogInformation("CloudflareChatClient sending request. Url: {Url} ModelId: {ModelId}", requestUrl, _modelId);
            }
            else
            {
                Console.WriteLine($"CloudflareChatClient sending request. Url: {requestUrl} ModelId: {_modelId}");
            }

            var response = await _httpClient.PostAsJsonAsync(url, request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var cloudflareResponse = await response.Content.ReadFromJsonAsync<CloudflareResponse>(cancellationToken: cancellationToken);

            var text = cloudflareResponse?.Result?.Response ?? "";
            var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, text));
            chatResponse.ModelId = _modelId;
            return chatResponse;
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var request = CreateRequest(chatMessages, options, stream: true);
            var url = $"https://api.cloudflare.com/client/v4/accounts/{_accountId}/ai/run/{_modelId}";

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(request)
            };

            using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new System.IO.StreamReader(stream);

            string? line;
            while ((line = await reader.ReadLineAsync()) is not null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!line.StartsWith("data: ")) continue;

                var json = line.Substring(6).Trim();
                if (json == "[DONE]") break;

                CloudflareStreamResponse? streamEvent = null;
                try
                {
                    streamEvent = JsonSerializer.Deserialize<CloudflareStreamResponse>(json);
                }
                catch { continue; }

                if (streamEvent?.Response != null)
                {
                    var update = new ChatResponseUpdate
                    {
                        Role = ChatRole.Assistant,
                        ModelId = _modelId
                    };
                    update.Contents.Add(new TextContent(streamEvent.Response));
                    yield return update;
                }
            }
        }

        private object CreateRequest(IEnumerable<ChatMessage> chatMessages, ChatOptions? options, bool stream)
        {
            var messages = new List<object>();
            foreach (var msg in chatMessages)
            {
                messages.Add(new
                {
                    role = msg.Role.Value,
                    content = msg.Text
                });
            }

            return new
            {
                messages = messages,
                stream = stream
            };
        }

        public void Dispose() => this._httpClient.Dispose();
        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        private sealed class CloudflareResponse
        {
            [JsonPropertyName("result")] public CloudflareResult? Result { get; set; }
        }

        private sealed class CloudflareResult
        {
            [JsonPropertyName("response")] public string? Response { get; set; }
        }

        private sealed class CloudflareStreamResponse
        {
            [JsonPropertyName("response")] public string? Response { get; set; }
        }
    }
}
