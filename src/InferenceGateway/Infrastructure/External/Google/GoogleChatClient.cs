// <copyright file="GoogleChatClient.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.External.Google
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
    using Microsoft.Extensions.Logging;

    public class GoogleChatClient : IChatClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _modelId;
        private readonly ChatClientMetadata _metadata;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        private const string Endpoint = "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions";
        private readonly ILogger<GoogleChatClient>? _logger;

        public GoogleChatClient(string apiKey, string modelId, HttpClient httpClient, ILogger<GoogleChatClient>? logger = null)
        {
            this._httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this._modelId = modelId ?? "default";
            this._logger = logger;
            if (!string.IsNullOrEmpty(apiKey))
            {
                this._httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            }
            this._httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Synaxis/1.0");
            this._metadata = new ChatClientMetadata("Google.Gemini", new Uri(Endpoint), this._modelId);
        }

        public ChatClientMetadata Metadata => this._metadata;

        public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            var requestObj = this.CreateRequest(chatMessages, options, stream: false);

            // Debug: print request URI and model field payload for diagnosing 404s and model formatting
            try
            {
                var requestJson = JsonSerializer.Serialize(requestObj, _jsonOptions);
                string payloadModel = this._modelId;
                try
                {
                    using var doc = JsonDocument.Parse(requestJson);
                    if (doc.RootElement.TryGetProperty("model", out var modelProp))
                    {
                        payloadModel = modelProp.ValueKind == JsonValueKind.String ? modelProp.GetString() ?? this._modelId : this._modelId;
                    }
                }
                catch { /* ignore parse errors and fall back to _modelId */ }

                if (this._logger != null)
                {
                    this._logger.LogInformation("GoogleChatClient sending request. Endpoint: {Endpoint} Model: {Model} Payload: {Payload}", Endpoint, payloadModel, requestJson);
                }
                else
                {
                    Console.WriteLine($"GoogleChatClient sending request. Endpoint: {Endpoint} Model: {payloadModel} Payload: {requestJson}");
                }
            }
            catch (Exception ex)
            {
                // Ensure diagnostic information never throws
                Console.WriteLine($"GoogleChatClient debug logging failed: {ex}");
            }

            var response = await this._httpClient.PostAsJsonAsync(Endpoint, requestObj, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new HttpRequestException($"Google Gemini API Error {response.StatusCode}: {err}");
            }

            var openAiResp = await response.Content.ReadFromJsonAsync<OpenAiChatResponse>(_jsonOptions, cancellationToken: cancellationToken);

            var choice = openAiResp?.Choices?.FirstOrDefault();
            var text = choice?.Message?.Content ?? choice?.Text ?? string.Empty;

            var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, text))
            {
                ModelId = this._modelId,
            };

            return chatResponse;
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Streaming responses are not implemented for GoogleChatClient yet.");
        }

        private object CreateRequest(IEnumerable<ChatMessage> chatMessages, ChatOptions? options, bool stream)
        {
            var messages = chatMessages.Select(m => new
            {
                role = m.Role == ChatRole.User ? "user" :
                       m.Role == ChatRole.Assistant ? "assistant" :
                       m.Role == ChatRole.System ? "system" : "user",
                content = m.Text,
            }).ToList();

            return new
            {
                model = options?.ModelId ?? this._modelId,
                messages = messages,
                stream = stream,
            };
        }

        public void Dispose() => this._httpClient.Dispose();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        private class OpenAiChatResponse
        {
            [JsonPropertyName("choices")] public OpenAiChoice[]? Choices { get; set; }
        }

        private class OpenAiChoice
        {
            [JsonPropertyName("message")] public OpenAiMessage? Message { get; set; }

            [JsonPropertyName("text")] public string? Text { get; set; }

            [JsonPropertyName("finish_reason")] public string? FinishReason { get; set; }
        }

        private class OpenAiMessage
        {
            [JsonPropertyName("role")] public string? Role { get; set; }

            [JsonPropertyName("content")] public string? Content { get; set; }
        }
    }
}
