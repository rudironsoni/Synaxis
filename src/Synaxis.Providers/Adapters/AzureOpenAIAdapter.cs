// <copyright file="AzureOpenAIAdapter.cs" company="Synaxis">
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
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Core;
    using Azure.Identity;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Synaxis.Contracts.V1.Errors;
    using Synaxis.Contracts.V1.Messages;
    using Synaxis.Providers.Configuration;
    using Synaxis.Providers.Exceptions;
    using Synaxis.Providers.Models;

    /// <summary>
    /// Azure OpenAI implementation of <see cref="IProviderAdapter"/>.
    /// </summary>
    public sealed class AzureOpenAIAdapter : IProviderAdapter, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly AzureOpenAIOptions _options;
        private readonly ILogger<AzureOpenAIAdapter> _logger;
        private readonly TokenCredential? _credential;
        private readonly SemaphoreSlim _tokenLock;
        private string? _cachedToken;
        private DateTimeOffset _tokenExpiry;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureOpenAIAdapter"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="options">The Azure OpenAI options.</param>
        /// <param name="logger">The logger.</param>
        public AzureOpenAIAdapter(
            HttpClient httpClient,
            IOptions<AzureOpenAIOptions> options,
            ILogger<AzureOpenAIAdapter> logger)
        {
            this._httpClient = httpClient!;
            ArgumentNullException.ThrowIfNull(options);
            this._options = options.Value;
            this._logger = logger!;

            this._tokenLock = new SemaphoreSlim(1, 1);
            this._tokenExpiry = DateTimeOffset.MinValue;

            if (this._options.UseAzureAd)
            {
                this._credential = new DefaultAzureCredential();
            }

            this.ConfigureHttpClient();
        }

        /// <inheritdoc/>
        public ProviderType ProviderType => ProviderType.AzureOpenAI;

        /// <inheritdoc/>
        public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var deploymentName = string.IsNullOrEmpty(this._options.ChatDeploymentName)
                ? request.Model
                : this._options.ChatDeploymentName;

            var openAIRequest = this.BuildChatRequest(request);
            var response = await this.PostWithRetryAsync<OpenAIChatRequest, OpenAIChatResponse>(
                $"openai/deployments/{deploymentName}/chat/completions",
                openAIRequest,
                cancellationToken).ConfigureAwait(false);

            return this.MapToChatResponse(response);
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<StreamingResponse> StreamChatAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var deploymentName = string.IsNullOrEmpty(this._options.ChatDeploymentName)
                ? request.Model
                : this._options.ChatDeploymentName;

            var openAIRequest = this.BuildChatRequest(request);
            openAIRequest.Stream = true;

            var endpoint = $"openai/deployments/{deploymentName}/chat/completions";
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(openAIRequest), Encoding.UTF8, "application/json"),
            };

            await this.SetAuthenticationHeaderAsync(requestMessage, cancellationToken).ConfigureAwait(false);

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

            var deploymentName = string.IsNullOrEmpty(this._options.EmbeddingDeploymentName)
                ? request.Model
                : this._options.EmbeddingDeploymentName;

            var openAIRequest = new OpenAIEmbeddingRequest
            {
                Model = request.Model,
                Input = request.Input.ToList(),
            };

            var response = await this.PostWithRetryAsync<OpenAIEmbeddingRequest, OpenAIEmbeddingResponse>(
                $"openai/deployments/{deploymentName}/embeddings",
                openAIRequest,
                cancellationToken).ConfigureAwait(false);

            return this.MapToEmbeddingResponse(response);
        }

        private void ConfigureHttpClient()
        {
            this._httpClient.BaseAddress = new Uri(this._options.Endpoint);
            this._httpClient.Timeout = TimeSpan.FromSeconds(this._options.TimeoutSeconds);
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

        private async Task SetAuthenticationHeaderAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (this._options.UseAzureAd)
            {
                var token = await this.GetAzureAdTokenAsync(cancellationToken).ConfigureAwait(false);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else if (!string.IsNullOrEmpty(this._options.ApiKey))
            {
                request.Headers.Add("api-key", this._options.ApiKey);
            }
        }

        private async Task<string> GetAzureAdTokenAsync(CancellationToken cancellationToken)
        {
            if (this._credential == null)
            {
                throw new InvalidOperationException("Azure AD credential is not configured.");
            }

            await this._tokenLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // Check if cached token is still valid (with 5-minute buffer)
                if (this._cachedToken != null && DateTimeOffset.UtcNow < this._tokenExpiry.AddMinutes(-5))
                {
                    return this._cachedToken;
                }

                // Request new token
                var tokenContext = new TokenRequestContext(new[] { "https://cognitiveservices.azure.com/.default" });
                var token = await this._credential.GetTokenAsync(tokenContext, cancellationToken).ConfigureAwait(false);

                this._cachedToken = token.Token;
                this._tokenExpiry = token.ExpiresOn;

                return this._cachedToken;
            }
            finally
            {
                this._tokenLock.Release();
            }
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
                    this._logger.LogWarning(ex, "Azure OpenAI API request failed (attempt {Attempt}/{MaxRetries})", attempt, this._options.MaxRetries);

                    if (attempt < this._options.MaxRetries)
                    {
                        var delay = AzureOpenAIAdapter.GetRetryDelay(attempt);
                        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                        continue;
                    }
                }
            }

            throw new ProviderException(
                new SynaxisError
                {
                    Code = "AzureOpenAI.MaxRetriesExceeded",
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
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = content,
            };

            await this.SetAuthenticationHeaderAsync(requestMessage, cancellationToken).ConfigureAwait(false);

            using var response = await this._httpClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
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
                    Code = $"AzureOpenAI.{statusCode}",
                    Message = openAIError?.Message ?? $"Azure OpenAI API error: {statusCode}",
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
                    Code = $"AzureOpenAI.{statusCode}",
                    Message = $"Azure OpenAI API error: {statusCode}",
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

        /// <summary>
        /// Disposes the resources used by the adapter.
        /// </summary>
        public void Dispose()
        {
            if (!this._disposed)
            {
                this._tokenLock.Dispose();
                this._disposed = true;
            }
        }

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
    }
}
