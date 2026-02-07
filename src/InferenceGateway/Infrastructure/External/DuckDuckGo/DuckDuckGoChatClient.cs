// <copyright file="DuckDuckGoChatClient.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.External.DuckDuckGo
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Json;
    using System.Runtime.CompilerServices;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.AI;

    public class DuckDuckGoChatClient : IChatClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _modelId;
        private readonly ChatClientMetadata _metadata;
        private string? _vqdToken;

        private static readonly string[] SupportedModels = new[] { "gpt-4o-mini", "claude-3-haiku", "llama-3.1-70b", "o3-mini" };

        public DuckDuckGoChatClient(HttpClient httpClient, string modelId)
        {
            this._httpClient = httpClient;
            this._modelId = modelId;
            this._metadata = new ChatClientMetadata("DuckDuckGo", new Uri("https://duckduckgo.com/"), modelId);
        }

        public ChatClientMetadata Metadata => this._metadata;

        public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            // Ensure we have a fresh token
            await this.EnsureTokenAsync(cancellationToken);

            var requestObj = this.CreateRequest(chatMessages);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://duckduckgo.com/duckchat/v1/chat")
            {
                Content = JsonContent.Create(requestObj, options: new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
            };

            if (!string.IsNullOrEmpty(this._vqdToken))
            {
                httpRequest.Headers.TryAddWithoutValidation("x-vqd-4", this._vqdToken);
            }
            httpRequest.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120 Safari/537.36");

            using var response = await this._httpClient.SendAsync(httpRequest, cancellationToken);

            // Update token from response headers for next call
            if (response.Headers.TryGetValues("x-vqd-4", out var vals))
            {
                this._vqdToken = vals.FirstOrDefault();
            }

            if (!response.IsSuccessStatusCode)
            {
                // Per user preference, return an empty response instead of throwing
                return new ChatResponse(new List<ChatMessage>());
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            // Attempt to parse simple response schema { reply: "..." } or { message: "..." }
            string? reply = null;
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("reply", out var r)) reply = r.GetString();
                else if (doc.RootElement.TryGetProperty("message", out var m)) reply = m.GetString();
                else if (doc.RootElement.ValueKind == JsonValueKind.String) reply = doc.RootElement.GetString();
                else
                {
                    // fallback: attempt to find first string value
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.String)
                        {
                            reply = prop.Value.GetString();
                            break;
                        }
                    }
                }
            }
            catch { reply = null; }

            var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, reply ?? string.Empty));
            chatResponse.ModelId = this._modelId;
            return chatResponse;
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            // DuckDuckGo streaming is not implemented; provide a simple single-message wrapper as async enumerable
            return this.ReturnSingleAsync(chatMessages, options, cancellationToken);
        }

        private async IAsyncEnumerable<ChatResponseUpdate> ReturnSingleAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var resp = await this.GetResponseAsync(chatMessages, options, cancellationToken);
            if (resp != null)
            {
                var update = new ChatResponseUpdate { Role = ChatRole.Assistant, ModelId = this._modelId };
                update.Contents.Add(new TextContent(resp.Messages.FirstOrDefault()?.Text ?? string.Empty));
                yield return update;
            }
        }

        private object CreateRequest(IEnumerable<ChatMessage> chatMessages)
        {
            var messages = chatMessages.Select(m => new
            {
                role = m.Role.Value,
                content = m.Text,
            }).ToList();

            var model = SupportedModels.Contains(this._modelId) ? this._modelId : this._modelId;

            return new { model = model, messages = messages };
        }

        private async Task EnsureTokenAsync(CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(this._vqdToken))
            {
                return;
            }

            using var response = await this._httpClient.GetAsync("https://duckduckgo.com/duckchat/v1/status", cancellationToken);
            if (response.Headers.TryGetValues("x-vqd-4", out var vals))
            {
                this._vqdToken = vals.FirstOrDefault();
            }
        }

        public void Dispose() => this._httpClient.Dispose();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
    }
}
