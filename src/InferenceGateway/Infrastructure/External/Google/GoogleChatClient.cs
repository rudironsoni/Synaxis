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

    /// <summary>
    /// GoogleChatClient class.
    /// </summary>
    public sealed class GoogleChatClient : IChatClient
    {
#pragma warning disable S1075 // URIs should not be hardcoded - API endpoint
        private const string Endpoint = "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions";
#pragma warning restore S1075 // URIs should not be hardcoded

        private readonly HttpClient _httpClient;
        private readonly string _modelId;
        private readonly ChatClientMetadata _metadata;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        private readonly ILogger<GoogleChatClient>? _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="GoogleChatClient"/> class.
        /// </summary>
        /// <param name="apiKey">The API key for Google Gemini.</param>
        /// <param name="modelId">The model identifier to use.</param>
        /// <param name="httpClient">The HTTP client for making requests.</param>
        /// <param name="logger">Optional logger instance.</param>
        public GoogleChatClient(string apiKey, string modelId, HttpClient httpClient, ILogger<GoogleChatClient>? logger = null)
        {
            ArgumentNullException.ThrowIfNull(httpClient);
            this._httpClient = httpClient;
            this._modelId = modelId ?? "default";
            this._logger = logger;
            if (!string.IsNullOrEmpty(apiKey))
            {
                this._httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            }

            this._httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Synaxis/1.0");
            this._metadata = new ChatClientMetadata("Google.Gemini", new Uri(Endpoint), this._modelId);
        }

        /// <summary>
        /// Gets the metadata for this chat client.
        /// </summary>
        public ChatClientMetadata Metadata => this._metadata;

        /// <inheritdoc/>
        public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            var requestObj = this.CreateRequest(messages, options, stream: false);

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
                catch
                {
                    /* ignore parse errors and fall back to _modelId */
                }

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

#pragma warning disable IDISP001 // HttpClient created by IHttpClientFactory
            var response = await this._httpClient.PostAsJsonAsync(Endpoint, requestObj, cancellationToken).ConfigureAwait(false);
#pragma warning restore IDISP001
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                throw new HttpRequestException($"Google Gemini API Error {response.StatusCode}: {err}");
            }

            var openAiResp = await response.Content.ReadFromJsonAsync<OpenAiChatResponse>(_jsonOptions, cancellationToken: cancellationToken).ConfigureAwait(false);

            var choice = openAiResp?.Choices?.FirstOrDefault();
            var text = choice?.Message?.Content ?? choice?.Text ?? string.Empty;

            var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, text))
            {
                ModelId = this._modelId,
            };

            return chatResponse;
        }

        /// <inheritdoc/>
        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Streaming responses are not implemented for GoogleChatClient yet.");
        }

        private object CreateRequest(IEnumerable<ChatMessage> messages, ChatOptions? options, bool stream)
        {
            var messageList = messages.Select(m =>
            {
                string role;
                if (m.Role == ChatRole.User)
                {
                    role = "user";
                }
                else if (m.Role == ChatRole.Assistant)
                {
                    role = "assistant";
                }
                else if (m.Role == ChatRole.System)
                {
                    role = "system";
                }
                else
                {
                    role = "user";
                }

                return new
                {
                    role = role,
                    content = m.Text,
                };
            }).ToList();

            return new
            {
                model = options?.ModelId ?? this._modelId,
                messages = messageList,
                stream = stream,
            };
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            // HttpClient is injected and should not be disposed here
        }

        /// <inheritdoc/>
        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        private sealed class OpenAiChatResponse
        {
            /// <summary>
            /// Gets or sets the Choices.
            /// </summary>
            [JsonPropertyName("choices")]
            public OpenAiChoice[]? Choices { get; set; }
        }

        private sealed class OpenAiChoice
        {
            /// <summary>
            /// Gets or sets the Message.
            /// </summary>
            [JsonPropertyName("message")]
            public OpenAiMessage? Message { get; set; }

            /// <summary>
            /// Gets or sets the Text.
            /// </summary>
            [JsonPropertyName("text")]
            public string? Text { get; set; }

            /// <summary>
            /// Gets or sets the FinishReason.
            /// </summary>
            [JsonPropertyName("finish_reason")]
            public string? FinishReason { get; set; }
        }

        private sealed class OpenAiMessage
        {
            /// <summary>
            /// Gets or sets the Role.
            /// </summary>
            [JsonPropertyName("role")]
            public string? Role { get; set; }

            /// <summary>
            /// Gets or sets the Content.
            /// </summary>
            [JsonPropertyName("content")]
            public string? Content { get; set; }
        }
    }
}
