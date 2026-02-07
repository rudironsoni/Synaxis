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

    public class PollinationsChatClient : IChatClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _modelId;
        private readonly ChatClientMetadata _metadata;

        public PollinationsChatClient(HttpClient httpClient, string? modelId = null)
        {
            this._httpClient = httpClient;
            this._modelId = modelId ?? "openai";
            this._metadata = new ChatClientMetadata("Pollinations", new Uri("https://text.pollinations.ai/"), _modelId);
        }

        public ChatClientMetadata Metadata => this._metadata;

        public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            var request = CreateRequest(chatMessages, options, stream: false);
            var response = await _httpClient.PostAsJsonAsync("https://text.pollinations.ai/", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, content));
            chatResponse.ModelId = _modelId;
            return chatResponse;
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var request = CreateRequest(chatMessages, options, stream: true);

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://text.pollinations.ai/")
            {
                Content = JsonContent.Create(request)
            };

            using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new System.IO.StreamReader(stream);

            // Pollinations streaming is just raw text updates if I recall correctly,
            // but if we use the POST endpoint it might be different.
            // Actually, Pollinations POST endpoint with stream: true returns SSE if requested,
            // but often it just returns chunks of text.

            char[] buffer = new char[1024];
            string? line;
            while ((line = await reader.ReadLineAsync()) is not null)
            {
                int read = await reader.ReadAsync(buffer, 0, buffer.Length);
                if (read > 0)
                {
                    var text = new string(buffer, 0, read);
                    var update = new ChatResponseUpdate
                    {
                        Role = ChatRole.Assistant,
                        ModelId = _modelId
                    };
                    update.Contents.Add(new TextContent(text));
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

            // Map standard model names to Pollinations aliases
            var model = _modelId switch
            {
                "gpt-4o-mini" => "openai",
                "gpt-4o" => "openai-large",
                _ => this._modelId
            };

            return new
            {
                messages = messages,
                model = model,
                stream = stream,
                seed = Random.Shared.Next() // Avoid caching
            };
        }

        public void Dispose() => this._httpClient.Dispose();
        public object? GetService(Type serviceType, object? serviceKey = null) => null;
    }
}
