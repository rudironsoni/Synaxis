// <copyright file="AnthropicClient.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.Anthropic
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Options;
    using Synaxis.Contracts.V1.Errors;
    using Synaxis.Contracts.V1.Messages;
    using Synaxis.Providers.Anthropic.Configuration;

    /// <summary>
    /// HTTP client wrapper for the Anthropic API.
    /// </summary>
    public sealed class AnthropicClient
    {
        private readonly HttpClient httpClient;
        private readonly AnthropicOptions options;
        private readonly JsonSerializerOptions jsonOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnthropicClient"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="options">The Anthropic configuration options.</param>
        public AnthropicClient(HttpClient httpClient, IOptions<AnthropicOptions> options)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            this.options.Validate();

            this.jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            };

            this.ConfigureHttpClient();
        }

        /// <summary>
        /// Sends a chat completion request to the Anthropic API.
        /// </summary>
        /// <param name="messages">The conversation messages.</param>
        /// <param name="model">The model to use.</param>
        /// <param name="maxTokens">The maximum number of tokens to generate.</param>
        /// <param name="temperature">The sampling temperature.</param>
        /// <param name="stream">Whether to stream the response.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the API response.</returns>
        public async Task<ChatResponse> ChatCompletionAsync(
            IEnumerable<ChatMessage> messages,
            string model,
            int? maxTokens = null,
            double? temperature = null,
            bool stream = false,
            CancellationToken cancellationToken = default)
        {
            var requestBody = BuildChatRequest(messages, model, maxTokens, temperature, stream);
            var requestJson = JsonSerializer.Serialize(requestBody, this.jsonOptions);
            using var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            using var response = await this.httpClient.PostAsync("messages", content, cancellationToken).ConfigureAwait(false);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw this.CreateExceptionFromResponse(response, responseJson);
            }

            var anthropicResponse = JsonSerializer.Deserialize<AnthropicChatResponse>(responseJson, this.jsonOptions);
            if (anthropicResponse == null)
            {
                throw new InvalidOperationException("Failed to deserialize Anthropic response.");
            }

            return this.MapToSynaxisResponse(anthropicResponse, model);
        }

        /// <summary>
        /// Sends a streaming chat completion request to the Anthropic API.
        /// </summary>
        /// <param name="messages">The conversation messages.</param>
        /// <param name="model">The model to use.</param>
        /// <param name="maxTokens">The maximum number of tokens to generate.</param>
        /// <param name="temperature">The sampling temperature.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>An async enumerable of chat response chunks.</returns>
        public async IAsyncEnumerable<string> ChatCompletionStreamAsync(
            IEnumerable<ChatMessage> messages,
            string model,
            int? maxTokens = null,
            double? temperature = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var requestBody = BuildChatRequest(messages, model, maxTokens, temperature, stream: true);
            var requestJson = JsonSerializer.Serialize(requestBody, this.jsonOptions);
            using var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            using var response = await this.httpClient.PostAsync("messages", content, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var errorJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                throw this.CreateExceptionFromResponse(response, errorJson);
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var reader = new System.IO.StreamReader(stream);

            while (!reader.EndOfStream)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:", StringComparison.Ordinal))
                {
                    continue;
                }

                var data = line.Substring(5).Trim();
                if (string.Equals(data, "[DONE]", StringComparison.Ordinal))
                {
                    break;
                }

                var chunk = JsonSerializer.Deserialize<AnthropicStreamChunk>(data, this.jsonOptions);
                if (chunk?.Delta?.Text != null && string.Equals(chunk.Type, "content_block_delta", StringComparison.Ordinal))
                {
                    yield return chunk.Delta.Text;
                }
            }
        }

        private void ConfigureHttpClient()
        {
            this.httpClient.BaseAddress = new Uri(this.options.BaseUrl);
            this.httpClient.DefaultRequestHeaders.Clear();
            this.httpClient.DefaultRequestHeaders.Add("x-api-key", this.options.ApiKey);

            if (!string.IsNullOrWhiteSpace(this.options.AnthropicVersion))
            {
                this.httpClient.DefaultRequestHeaders.Add("anthropic-version", this.options.AnthropicVersion);
            }

            this.httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        private static object BuildChatRequest(
            IEnumerable<ChatMessage> messages,
            string model,
            int? maxTokens,
            double? temperature,
            bool stream)
        {
            var messagesList = messages.ToList();
            var systemMessage = messagesList.FirstOrDefault(m => string.Equals(m.Role, "system", StringComparison.OrdinalIgnoreCase));
            var conversationMessages = messagesList.Where(m => !string.Equals(m.Role, "system", StringComparison.OrdinalIgnoreCase)).ToList();

            var request = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["model"] = model,
                ["messages"] = conversationMessages.Select(m => new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["role"] = m.Role.ToLowerInvariant(),
                    ["content"] = m.Content,
                }).ToList(),
                ["max_tokens"] = maxTokens ?? 1024,
                ["stream"] = stream,
            };

            if (systemMessage != null)
            {
                request["system"] = systemMessage.Content;
            }

            if (temperature.HasValue)
            {
                request["temperature"] = temperature.Value;
            }

            return request;
        }

        private ChatResponse MapToSynaxisResponse(AnthropicChatResponse anthropicResponse, string model)
        {
            var message = new ChatMessage
            {
                Role = anthropicResponse.Role ?? "assistant",
                Content = anthropicResponse.Content?.FirstOrDefault()?.Text ?? string.Empty,
            };

            var choice = new ChatChoice
            {
                Index = 0,
                Message = message,
                FinishReason = anthropicResponse.StopReason,
            };

            var usage = anthropicResponse.Usage != null
                ? new ChatUsage
                {
                    PromptTokens = anthropicResponse.Usage.InputTokens,
                    CompletionTokens = anthropicResponse.Usage.OutputTokens,
                    TotalTokens = anthropicResponse.Usage.InputTokens + anthropicResponse.Usage.OutputTokens,
                }
                : null;

            return new ChatResponse
            {
                Id = anthropicResponse.Id ?? Guid.NewGuid().ToString(),
                Object = "chat.completion",
                Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Model = model,
                Choices = new[] { choice },
                Usage = usage,
            };
        }

        private Exception CreateExceptionFromResponse(HttpResponseMessage response, string responseJson)
        {
            var statusCode = (int)response.StatusCode;
            var errorMessage = $"Anthropic API request failed with status code {statusCode}.";

            try
            {
                var errorResponse = JsonSerializer.Deserialize<AnthropicErrorResponse>(responseJson, this.jsonOptions);
                if (errorResponse?.Error != null)
                {
                    errorMessage = $"{errorResponse.Error.Type}: {errorResponse.Error.Message}";
                }
            }
            catch
            {
                // Use default error message if JSON parsing fails
            }

            var synaxisError = this.MapHttpStatusToSynaxisError(statusCode, errorMessage);
            return new HttpRequestException($"{synaxisError.Code}: {synaxisError.Message}", null, response.StatusCode);
        }

        private SynaxisError MapHttpStatusToSynaxisError(int statusCode, string message)
        {
            return statusCode switch
            {
                401 => new SynaxisError
                {
                    Code = "AUTH_INVALID",
                    Message = message,
                    Severity = ErrorSeverity.Error,
                    Category = ErrorCategory.Auth,
                },
                403 => new SynaxisError
                {
                    Code = "AUTH_FORBIDDEN",
                    Message = message,
                    Severity = ErrorSeverity.Error,
                    Category = ErrorCategory.Auth,
                },
                429 => new SynaxisError
                {
                    Code = "RATE_LIMIT_EXCEEDED",
                    Message = message,
                    Severity = ErrorSeverity.Error,
                    Category = ErrorCategory.RateLimit,
                },
                400 => new SynaxisError
                {
                    Code = "VALIDATION_ERROR",
                    Message = message,
                    Severity = ErrorSeverity.Error,
                    Category = ErrorCategory.Validation,
                },
                _ => new SynaxisError
                {
                    Code = "PROVIDER_ERROR",
                    Message = message,
                    Severity = ErrorSeverity.Error,
                    Category = ErrorCategory.Provider,
                },
            };
        }

        // Anthropic-specific response models
        private sealed class AnthropicChatResponse
        {
            public string? Id { get; set; }

            public string? Type { get; set; }

            public string? Role { get; set; }

            public List<AnthropicContent>? Content { get; set; }

            public string? StopReason { get; set; }

            public AnthropicUsage? Usage { get; set; }
        }

        private sealed class AnthropicContent
        {
            public string? Type { get; set; }

            public string? Text { get; set; }
        }

        private sealed class AnthropicUsage
        {
            public int InputTokens { get; set; }

            public int OutputTokens { get; set; }
        }

        private sealed class AnthropicStreamChunk
        {
            public string? Type { get; set; }

            public AnthropicDelta? Delta { get; set; }
        }

        private sealed class AnthropicDelta
        {
            public string? Type { get; set; }

            public string? Text { get; set; }
        }

        private sealed class AnthropicErrorResponse
        {
            public AnthropicError? Error { get; set; }
        }

        private sealed class AnthropicError
        {
            public string? Type { get; set; }

            public string? Message { get; set; }
        }
    }
}
