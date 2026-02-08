// <copyright file="CohereChatClient.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure
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

    /// <summary>
    /// Cohere chat client implementation.
    /// </summary>
    /// <summary>
    /// CohereChatClient class.
    /// </summary>
    public sealed class CohereChatClient : IChatClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _modelId;
        private readonly ChatClientMetadata _metadata;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="CohereChatClient"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="modelId">The model ID.</param>
        /// <param name="apiKey">The API key.</param>
        public CohereChatClient(HttpClient httpClient, string modelId, string apiKey)
        {
            this._httpClient = httpClient;
            this._modelId = modelId;
            this._httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            this._httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Synaxis/1.0");
#pragma warning disable S1075 // URIs should not be hardcoded - API endpoint
            this._metadata = new ChatClientMetadata("Cohere", new Uri("https://api.cohere.com/v2/chat"), modelId);
#pragma warning restore S1075 // URIs should not be hardcoded
        }

        /// <summary>
        /// Gets the metadata for this chat client.
        /// </summary>
        public ChatClientMetadata Metadata => this._metadata;

        /// <inheritdoc/>
        public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            var requestObj = this.CreateRequest(messages, options, stream: false);
#pragma warning disable S1075 // URIs should not be hardcoded - API endpoint
#pragma warning disable IDISP001 // HttpRequestMessage created and disposed within using block
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.cohere.com/v2/chat")
#pragma warning restore S1075 // URIs should not be hardcoded
#pragma warning restore IDISP001
            {
                Content = JsonContent.Create(requestObj),
            };

            using var response = await this._httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                throw new HttpRequestException($"Cohere API Error {response.StatusCode}: {error}");
            }

            response.EnsureSuccessStatusCode();

            var cohereResponse = await response.Content.ReadFromJsonAsync<CohereResponseV2>(_jsonOptions, cancellationToken: cancellationToken).ConfigureAwait(false);

            var text = cohereResponse?.Message?.Content?.FirstOrDefault(c => string.Equals(c.Type, "text", StringComparison.Ordinal))?.Text ?? string.Empty;
            var chatResponse = new ChatResponse(new ChatMessage(ChatRole.Assistant, text))
            {
                ModelId = this._modelId,
            };
            return chatResponse;
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var requestObj = this.CreateRequest(messages, options, stream: true);
#pragma warning disable IDISP001
            var request = this.CreateStreamRequest(requestObj);
#pragma warning restore IDISP001

            using var response = await this._httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                throw new HttpRequestException($"Cohere API Error {response.StatusCode}: {err}");
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var reader = new System.IO.StreamReader(stream);

            await foreach (var update in this.ProcessStreamEvents(reader, cancellationToken).ConfigureAwait(false))
            {
                yield return update;
            }
        }

        private HttpRequestMessage CreateStreamRequest(object requestObj)
        {
#pragma warning disable S1075 // URIs should not be hardcoded - API endpoint
#pragma warning disable IDISP001 // HttpRequestMessage created and disposed within using block
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.cohere.com/v2/chat")
#pragma warning restore S1075 // URIs should not be hardcoded
#pragma warning restore IDISP001
            {
                Content = JsonContent.Create(requestObj),
            };

            // Ask for SSE
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

            return request;
        }

        private async IAsyncEnumerable<ChatResponseUpdate> ProcessStreamEvents(System.IO.StreamReader reader, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            string? line;
            string? currentEvent = null;

            while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) is not null)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                // Track event name if provided
                if (line.StartsWith("event: ", StringComparison.OrdinalIgnoreCase))
                {
                    currentEvent = line.Substring(7).Trim();
                    continue;
                }

                if (!line.StartsWith("data: ", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var json = line.Substring(6).Trim();
                if (string.Equals(json, "[DONE]", StringComparison.Ordinal))
                {
                    break;
                }

                var ev = CohereChatClient.DeserializeStreamEvent(json);
                if (ev == null)
                {
                    continue;
                }

                var evName = currentEvent ?? ev.Type;

                // Handle content delta events
                var contentUpdate = this.CreateContentUpdate(evName, ev);
                if (contentUpdate != null)
                {
                    yield return contentUpdate;
                }

                // Handle message end events
                var endUpdate = this.CreateMessageEndUpdate(evName, ev);
                if (endUpdate != null)
                {
                    yield return endUpdate;
                }
            }
        }

        private static CohereStreamEvent? DeserializeStreamEvent(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<CohereStreamEvent>(json, _jsonOptions);
            }
            catch (JsonException)
            {
                // ignore malformed json
                return null;
            }
        }

        private ChatResponseUpdate? CreateContentUpdate(string? eventName, CohereStreamEvent ev)
        {
            if (string.Equals(eventName, "content-delta", StringComparison.OrdinalIgnoreCase) || ev?.Delta?.Message?.Content != null)
            {
                var contents = ev?.Delta?.Message?.Content;
                if (contents != null)
                {
                    var textParts = contents.Where(c => !string.IsNullOrEmpty(c.Text)).Select(c => c.Text).ToList();
                    if (textParts.Count > 0)
                    {
                        var text = string.Join(string.Empty, textParts);
                        var update = new ChatResponseUpdate
                        {
                            Role = ChatRole.Assistant,
                            ModelId = this._modelId,
                        };
                        update.Contents.Add(new TextContent(text));
                        return update;
                    }
                }
            }

            return null;
        }

        private ChatResponseUpdate? CreateMessageEndUpdate(string? eventName, CohereStreamEvent ev)
        {
            if (string.Equals(eventName, "message-end", StringComparison.OrdinalIgnoreCase) || ev?.Delta?.FinishReason != null)
            {
                var finish = ev?.Delta?.FinishReason ?? ev?.Delta?.Message?.Content?.FirstOrDefault()?.Type;
                var update = new ChatResponseUpdate
                {
                    Role = ChatRole.Assistant,
                    ModelId = this._modelId,
                };

                TrySetFinishReason(update, finish);
                return update;
            }

            return null;
        }

        private static void TrySetFinishReason(ChatResponseUpdate update, string? finishReason)
        {
            try
            {
                var prop = typeof(ChatResponseUpdate).GetProperty("FinishReason");
                if (prop != null && prop.PropertyType == typeof(string))
                {
                    prop.SetValue(update, finishReason);
                }
            }
            catch
            {
                /* ignore any reflection issues */
            }
        }

        private object CreateRequest(IEnumerable<ChatMessage> chatMessages, ChatOptions? options, bool stream)
        {
            var messages = chatMessages.Select(m => new
            {
                role = MapChatRole(m.Role),
                content = m.Text,
            }).ToList();

            return new
            {
                model = options?.ModelId ?? this._modelId,
                messages = messages,
                stream = stream,
            };
        }

        private static string MapChatRole(ChatRole role)
        {
            if (role == ChatRole.User)
            {
                return "user";
            }

            if (role == ChatRole.Assistant)
            {
                return "assistant";
            }

            if (role == ChatRole.System)
            {
                return "system";
            }

            return "user";
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            // HttpClient is injected and should not be disposed by this class
        }

        /// <inheritdoc/>
        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        // V2 Response Classes
        private sealed class CohereResponseV2
        {
            /// <summary>
            /// Gets or sets the Message.
            /// </summary>
            public CohereMessageV2? Message { get; set; }
        }

        private sealed class CohereMessageV2
        {
            public List<CohereContentV2>? Content { get; set; }
        }

        private sealed class CohereContentV2
        {
            /// <summary>
            /// Gets or sets the Type.
            /// </summary>
            public string? Type { get; set; }

            /// <summary>
            /// Gets or sets the Text.
            /// </summary>
            public string? Text { get; set; }
        }

        // Streaming event DTOs
        private sealed class CohereStreamEvent
        {
            /// <summary>
            /// Gets or sets the Type.
            /// </summary>
            [JsonPropertyName("type")]
            public string? Type { get; set; }

            /// <summary>
            /// Gets or sets the Delta.
            /// </summary>
            [JsonPropertyName("delta")]
            public CohereDelta? Delta { get; set; }

            /// <summary>
            /// Gets or sets the Usage.
            /// </summary>
            [JsonPropertyName("usage")]
            public CohereUsage? Usage { get; set; }
        }

        private sealed class CohereDelta
        {
            /// <summary>
            /// Gets or sets the Message.
            /// </summary>
            [JsonPropertyName("message")]
            public CohereDeltaMessage? Message { get; set; }

            /// <summary>
            /// Gets or sets the FinishReason.
            /// </summary>
            [JsonPropertyName("finish_reason")]
            public string? FinishReason { get; set; }
        }

        private sealed class CohereDeltaMessage
        {
            [JsonPropertyName("content")]
            public List<CohereContentV2>? Content { get; set; }
        }

        private sealed class CohereUsage
        {
            /// <summary>
            /// Gets or sets the PromptTokens.
            /// </summary>
            [JsonPropertyName("prompt_tokens")]
            public int? PromptTokens { get; set; }

            /// <summary>
            /// Gets or sets the CompletionTokens.
            /// </summary>
            [JsonPropertyName("completion_tokens")]
            public int? CompletionTokens { get; set; }

            /// <summary>
            /// Gets or sets the TotalTokens.
            /// </summary>
            [JsonPropertyName("total_tokens")]
            public int? TotalTokens { get; set; }
        }
    }
}
