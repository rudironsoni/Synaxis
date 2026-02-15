// <copyright file="OpenAIAdapter.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.Adapters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Http.Json;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Synaxis.Contracts.V1.Errors;
    using Synaxis.Contracts.V1.Messages;
    using Synaxis.Providers.Configuration;
    using Synaxis.Providers.Exceptions;
    using Synaxis.Providers.Models;

    /// <summary>
    /// OpenAI implementation of <see cref="IProviderAdapter"/>.
    /// </summary>
    public sealed class OpenAIAdapter : IProviderAdapter
    {
        private readonly HttpClient _httpClient;
        private readonly OpenAIOptions _options;
        private readonly ILogger<OpenAIAdapter> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenAIAdapter"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="options">The OpenAI options.</param>
        /// <param name="logger">The logger.</param>
        public OpenAIAdapter(
            HttpClient httpClient,
            IOptions<OpenAIOptions> options,
            ILogger<OpenAIAdapter> logger)
        {
            this._httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this._options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this.ConfigureHttpClient();
        }

        /// <inheritdoc/>
        public ProviderType ProviderType => ProviderType.OpenAI;

        /// <inheritdoc/>
        public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var openAIRequest = this.BuildChatRequest(request);
            var response = await this.PostWithRetryAsync<OpenAIChatRequest, OpenAIChatResponse>(
                "chat/completions",
                openAIRequest,
                cancellationToken).ConfigureAwait(false);

            return this.MapToChatResponse(response);
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<StreamingResponse> StreamChatAsync(ChatRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var openAIRequest = this.BuildChatRequest(request);
            openAIRequest.Stream = true;

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
            {
                Content = new StringContent(JsonSerializer.Serialize(openAIRequest), Encoding.UTF8, "application/json")
            };

            using var response = await this._httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var reader = new System.IO.StreamReader(stream);

            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: ", StringComparison.Ordinal))
                {
                    continue;
                }

                var data = line.Substring(6);
                if (string.Equals(data, "[DONE]", StringComparison.Ordinal))
                {
                    break;
                }

                OpenAIChatStreamChunk? chunk = null;
                try
                {
                    chunk = JsonSerializer.Deserialize<OpenAIChatStreamChunk>(data);
                }
                catch (JsonException ex)
                {
                    this._logger.LogWarning(ex, "Failed to parse streaming chunk: {Data}", data);
                    continue;
                }

                if (chunk is null)
                {
                    continue;
                }

                var isFinished = chunk.Choices.FirstOrDefault()?.FinishReason is not null;
                var streamingResponse = new StreamingResponse
                {
                    Id = chunk.Id,
                    Object = chunk.Object,
                    Created = chunk.Created,
                    Model = chunk.Model,
                    Choices = chunk.Choices.Select(c => new ChatChoice
                    {
                        Index = c.Index,
                        Message = new ChatMessage
                        {
                            Role = c.Delta?.Role ?? "assistant",
                            Content = c.Delta?.Content ?? string.Empty,
                        },
                        FinishReason = c.FinishReason,
                    }).ToArray(),
                    IsFinished = isFinished,
                };

                yield return streamingResponse;
            }
        }

        /// <inheritdoc/>
        public async Task<EmbeddingResponse> EmbedAsync(EmbedRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var openAIRequest = new OpenAIEmbeddingRequest
            {
                Model = request.Model,
                Input = request.Input.ToList(),
            };

            var response = await this.PostWithRetryAsync<OpenAIEmbeddingRequest, OpenAIEmbeddingResponse>(
                "embeddings",
                openAIRequest,
                cancellationToken).ConfigureAwait(false);

            return this.MapToEmbeddingResponse(response);
        }

        private void ConfigureHttpClient()
        {
            this._httpClient.BaseAddress = new Uri(this._options.BaseUrl);
            this._httpClient.Timeout = TimeSpan.FromSeconds(this._options.TimeoutSeconds);
            this._httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this._options.ApiKey);

            if (!string.IsNullOrWhiteSpace(this._options.OrganizationId))
            {
                this._httpClient.DefaultRequestHeaders.Add("OpenAI-Organization", this._options.OrganizationId);
            }
        }

        private OpenAIChatRequest BuildChatRequest(ChatRequest request)
        {
            return new OpenAIChatRequest
            {
                Model = request.Model,
                Messages = request.Messages.Select(m => new OpenAIMessage
                {
                    Role = m.Role,
                    Content = m.Content,
                    Name = m.Name,
                }).ToList(),
                Temperature = request.Temperature,
                MaxTokens = request.MaxTokens,
                TopP = request.TopP,
                FrequencyPenalty = request.FrequencyPenalty,
                PresencePenalty = request.PresencePenalty,
                Stop = request.Stop,
                Stream = false,
            };
        }

        private async Task<TResponse> PostWithRetryAsync<TRequest, TResponse>(
            string endpoint,
            TRequest request,
            CancellationToken cancellationToken)
        {
            var attempt = 0;
            Exception? lastException = null;

            while (attempt < this._options.MaxRetries)
            {
                attempt++;

                try
                {
                    return await this.ExecutePostAsync<TRequest, TResponse>(endpoint, request, cancellationToken).ConfigureAwait(false);
                }
                catch (ProviderException ex) when (ex.Error.Category is ErrorCategory.RateLimit or ErrorCategory.Provider)
                {
                    lastException = ex;
                    this._logger.LogWarning(ex, "OpenAI API request failed (attempt {Attempt}/{MaxRetries})", attempt, this._options.MaxRetries);

                    if (attempt < this._options.MaxRetries)
                    {
                        var delay = OpenAIAdapter.GetRetryDelay(attempt);
                        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                        continue;
                    }
                }
            }

            throw new ProviderException(
                new SynaxisError
                {
                    Code = "OpenAI.MaxRetriesExceeded",
                    Message = $"Maximum retry attempts ({this._options.MaxRetries}) exceeded",
                    Severity = ErrorSeverity.Error,
                    Category = ErrorCategory.Provider,
                },
                lastException ?? new Exception("Unknown error"));
        }

        private async Task<TResponse> ExecutePostAsync<TRequest, TResponse>(
            string endpoint,
            TRequest request,
            CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(request);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await this._httpClient.PostAsync(endpoint, content, cancellationToken).ConfigureAwait(false);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<TResponse>(responseContent);
                return result ?? throw new InvalidOperationException("Failed to deserialize response");
            }

            var error = this.HandleErrorResponse(response.StatusCode, responseContent);

            // Don't retry on certain errors
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest)
            {
                throw new ProviderException(error);
            }

            // For rate limit or server errors, throw to trigger retry
            throw new ProviderException(error);
        }

        private SynaxisError HandleErrorResponse(HttpStatusCode statusCode, string responseContent)
        {
            try
            {
                var errorResponse = JsonSerializer.Deserialize<OpenAIErrorResponse>(responseContent);
                var openAIError = errorResponse?.Error;

                var category = statusCode switch
                {
                    HttpStatusCode.Unauthorized => ErrorCategory.Auth,
                    HttpStatusCode.TooManyRequests => ErrorCategory.RateLimit,
                    HttpStatusCode.BadRequest => ErrorCategory.Validation,
                    _ => ErrorCategory.Provider,
                };

                return new SynaxisError
                {
                    Code = $"OpenAI.{statusCode}",
                    Message = openAIError?.Message ?? $"OpenAI API error: {statusCode}",
                    Severity = ErrorSeverity.Error,
                    Category = category,
                    Details = new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        ["StatusCode"] = (int)statusCode,
                        ["Type"] = openAIError?.Type ?? "unknown",
                        ["Code"] = openAIError?.Code ?? "unknown",
                    },
                };
            }
            catch
            {
                return new SynaxisError
                {
                    Code = $"OpenAI.{statusCode}",
                    Message = $"OpenAI API error: {statusCode}",
                    Severity = ErrorSeverity.Error,
                    Category = ErrorCategory.Provider,
                    Details = new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        ["StatusCode"] = (int)statusCode,
                        ["ResponseContent"] = responseContent,
                    },
                };
            }
        }

        private ChatResponse MapToChatResponse(OpenAIChatResponse response)
        {
            var choices = response.Choices
                .Select(c => new ChatChoice
                {
                    Index = c.Index,
                    Message = new ChatMessage
                    {
                        Role = c.Message?.Role ?? "assistant",
                        Content = c.Message?.Content ?? string.Empty,
                        Name = c.Message?.Name,
                    },
                    FinishReason = c.FinishReason,
                })
                .ToArray();

            ChatUsage? usage = null;
            if (response.Usage is not null)
            {
                usage = new ChatUsage
                {
                    PromptTokens = response.Usage.PromptTokens,
                    CompletionTokens = response.Usage.CompletionTokens,
                    TotalTokens = response.Usage.TotalTokens,
                };
            }

            return new ChatResponse
            {
                Id = response.Id,
                Object = response.Object,
                Created = response.Created,
                Model = response.Model,
                Choices = choices,
                Usage = usage,
            };
        }

        private EmbeddingResponse MapToEmbeddingResponse(OpenAIEmbeddingResponse response)
        {
            var data = response.Data
                .Select(d => new EmbeddingData
                {
                    Index = d.Index,
                    Embedding = d.Embedding.ToArray(),
                    Object = d.Object,
                })
                .ToArray();

            EmbeddingUsage? usage = null;
            if (response.Usage is not null)
            {
                usage = new EmbeddingUsage
                {
                    PromptTokens = response.Usage.PromptTokens,
                    TotalTokens = response.Usage.TotalTokens,
                };
            }

            return new EmbeddingResponse
            {
                Object = response.Object,
                Data = data,
                Usage = usage,
            };
        }

        private static int GetRetryDelay(int attempt)
        {
            // Exponential backoff: 1s, 2s, 4s
            return (int)Math.Pow(2, attempt - 1) * 1000;
        }

        #region OpenAI Models

        private sealed class OpenAIChatRequest
        {
            public string Model { get; init; } = string.Empty;

            public List<OpenAIMessage> Messages { get; init; } = new List<OpenAIMessage>();

            public double? Temperature { get; init; }

            public int? MaxTokens { get; init; }

            public double? TopP { get; init; }

            public double? FrequencyPenalty { get; init; }

            public double? PresencePenalty { get; init; }

            public string[]? Stop { get; init; }

            public bool Stream { get; set; }
        }

        private sealed class OpenAIMessage
        {
            public string Role { get; init; } = string.Empty;

            public string Content { get; init; } = string.Empty;

            public string? Name { get; init; }
        }

        private sealed class OpenAIChatResponse
        {
            public string Id { get; init; } = string.Empty;

            public string Object { get; init; } = string.Empty;

            public long Created { get; init; }

            public string Model { get; init; } = string.Empty;

            public List<OpenAIChoice> Choices { get; init; } = new List<OpenAIChoice>();

            public OpenAIUsage? Usage { get; init; }
        }

        private sealed class OpenAIChatStreamChunk
        {
            public string Id { get; init; } = string.Empty;

            public string Object { get; init; } = string.Empty;

            public long Created { get; init; }

            public string Model { get; init; } = string.Empty;

            public List<OpenAIStreamChoice> Choices { get; init; } = new List<OpenAIStreamChoice>();
        }

        private sealed class OpenAIChoice
        {
            public int Index { get; init; }

            public OpenAIMessage? Message { get; init; }

            public string? FinishReason { get; init; }
        }

        private sealed class OpenAIStreamChoice
        {
            public int Index { get; init; }

            public OpenAIStreamDelta? Delta { get; init; }

            public string? FinishReason { get; init; }
        }

        private sealed class OpenAIStreamDelta
        {
            public string? Role { get; init; }

            public string? Content { get; init; }
        }

        private sealed class OpenAIUsage
        {
            public int PromptTokens { get; init; }

            public int CompletionTokens { get; init; }

            public int TotalTokens { get; init; }
        }

        private sealed class OpenAIEmbeddingRequest
        {
            public string Model { get; init; } = string.Empty;

            public List<string> Input { get; init; } = new List<string>();
        }

        private sealed class OpenAIEmbeddingResponse
        {
            public string Object { get; init; } = string.Empty;

            public List<OpenAIEmbeddingData> Data { get; init; } = new List<OpenAIEmbeddingData>();

            public OpenAIEmbeddingUsage? Usage { get; init; }
        }

        private sealed class OpenAIEmbeddingData
        {
            public int Index { get; init; }

            public IReadOnlyList<float> Embedding { get; init; } = System.Array.Empty<float>();

            public string Object { get; init; } = string.Empty;
        }

        private sealed class OpenAIEmbeddingUsage
        {
            public int PromptTokens { get; init; }

            public int TotalTokens { get; init; }
        }

        private sealed class OpenAIErrorResponse
        {
            public OpenAIErrorDetail? Error { get; init; }
        }

        private sealed class OpenAIErrorDetail
        {
            public string Message { get; init; } = string.Empty;

            public string Type { get; init; } = string.Empty;

            public string Code { get; init; } = string.Empty;
        }

        #endregion
    }
}
