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

    public class AiHordeChatClient : IChatClient
    {
        private readonly HttpClient _httpClient;
        private readonly ChatClientMetadata _metadata;
        private readonly string _apiKey;

        private const string GenerateUrl = "https://stablehorde.net/api/v2/generate/text/async";
        private const string StatusUrlTemplate = "https://stablehorde.net/api/v2/generate/text/status/{0}";

        public AiHordeChatClient(HttpClient? httpClient = null, string apiKey = "0000000000")
        {
            _httpClient = httpClient ?? new HttpClient();
            _apiKey = apiKey;
            _metadata = new ChatClientMetadata("AiHorde", new Uri(GenerateUrl), "aihorde");
        }

        public ChatClientMetadata Metadata => _metadata;

        public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            var prompt = BuildPrompt(chatMessages);

            var request = new { prompt = prompt, models = new[] { "stable" } };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, GenerateUrl);
            httpRequest.Headers.TryAddWithoutValidation("apikey", _apiKey);
            httpRequest.Content = JsonContent.Create(request);

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            var genResp = await response.Content.ReadFromJsonAsync<GenerateResponse>(cancellationToken: cancellationToken);
            if (genResp == null || string.IsNullOrEmpty(genResp.Id))
                return new ChatResponse(new List<ChatMessage>());

            var id = genResp.Id;

            // Poll status until done
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(200, cancellationToken);
                using var statusReq = new HttpRequestMessage(HttpMethod.Get, string.Format(StatusUrlTemplate, id));
                statusReq.Headers.TryAddWithoutValidation("apikey", _apiKey);
                using var statusResp = await _httpClient.SendAsync(statusReq, cancellationToken);
                statusResp.EnsureSuccessStatusCode();

                var status = await statusResp.Content.ReadFromJsonAsync<StatusResponse>(cancellationToken: cancellationToken);
                if (status == null) break;
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

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // We'll implement polling and yield final result when ready
            var prompt = BuildPrompt(chatMessages);
            var request = new { prompt = prompt, models = new[] { "stable" } };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, GenerateUrl);
            httpRequest.Headers.TryAddWithoutValidation("apikey", _apiKey);
            httpRequest.Content = JsonContent.Create(request);

            using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            response.EnsureSuccessStatusCode();
            var genResp = await response.Content.ReadFromJsonAsync<GenerateResponse>(cancellationToken: cancellationToken);
            if (genResp == null || string.IsNullOrEmpty(genResp.Id)) yield break;

            var id = genResp.Id;

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(200, cancellationToken);
                using var statusReq = new HttpRequestMessage(HttpMethod.Get, string.Format(StatusUrlTemplate, id));
                statusReq.Headers.TryAddWithoutValidation("apikey", _apiKey);
                using var statusResp = await _httpClient.SendAsync(statusReq, cancellationToken);
                statusResp.EnsureSuccessStatusCode();
                var status = await statusResp.Content.ReadFromJsonAsync<StatusResponse>(cancellationToken: cancellationToken);
                if (status == null) yield break;
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

        private string BuildPrompt(IEnumerable<ChatMessage> messages)
        {
            var parts = new List<string>();
            foreach (var m in messages)
            {
                parts.Add($"[{m.Role.Value}] {m.Text}");
            }
            return string.Join("\n", parts);
        }

        public void Dispose() { }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        private sealed class GenerateResponse
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }
        }

        private sealed class StatusResponse
        {
            [JsonPropertyName("done")]
            public bool Done { get; set; }

            [JsonPropertyName("text")]
            public string? Text { get; set; }
        }
    }
}