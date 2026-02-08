// <copyright file="OpenAIClient.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.OpenAI
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Synaxis.Contracts.V1.Errors;
    using Synaxis.Providers.OpenAI.Configuration;
    using Synaxis.Providers.OpenAI.Models;

    /// <summary>
    /// HTTP client wrapper for OpenAI API calls.
    /// </summary>
    public sealed class OpenAIClient
    {
        private readonly HttpClient _httpClient;
        private readonly OpenAIOptions _options;
        private readonly ILogger<OpenAIClient> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenAIClient"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="options">The OpenAI options.</param>
        /// <param name="logger">The logger.</param>
        public OpenAIClient(
            HttpClient httpClient,
            IOptions<OpenAIOptions> options,
            ILogger<OpenAIClient> logger)
        {
            this._httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this._options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));

            this.ConfigureHttpClient();
        }

        /// <summary>
        /// Sends a POST request to the specified endpoint.
        /// </summary>
        /// <typeparam name="TRequest">The request type.</typeparam>
        /// <typeparam name="TResponse">The response type.</typeparam>
        /// <param name="endpoint">The API endpoint.</param>
        /// <param name="request">The request payload.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response.</returns>
        public async Task<TResponse> PostAsync<TRequest, TResponse>(
            string endpoint,
            TRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(request);

            var attempt = 0;
            Exception? lastException = null;

            while (attempt < this._options.MaxRetries)
            {
                attempt++;

                try
                {
                    return await this.ExecutePostRequestAsync<TRequest, TResponse>(endpoint, request, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException and not OpenAIException)
                {
                    lastException = ex;
                    this._logger.LogWarning(ex, "OpenAI API request failed (attempt {Attempt}/{MaxRetries})", attempt, this._options.MaxRetries);

                    if (attempt < this._options.MaxRetries)
                    {
                        var delay = GetRetryDelay(attempt);
                        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                        continue;
                    }
                }
            }

            throw new OpenAIException(
                new SynaxisError
                {
                    Code = "OpenAI.MaxRetriesExceeded",
                    Message = $"Maximum retry attempts ({this._options.MaxRetries}) exceeded",
                    Severity = ErrorSeverity.Error,
                    Category = ErrorCategory.Provider,
                },
                lastException);
        }

        /// <summary>
        /// Sends a POST request for multipart form data (e.g., audio transcription).
        /// </summary>
        /// <typeparam name="TResponse">The response type.</typeparam>
        /// <param name="endpoint">The API endpoint.</param>
        /// <param name="content">The multipart form content.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response.</returns>
        public async Task<TResponse> PostMultipartAsync<TResponse>(
            string endpoint,
            MultipartFormDataContent content,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(endpoint);
            ArgumentNullException.ThrowIfNull(content);

            using var response = await this._httpClient.PostAsync(endpoint, content, cancellationToken).ConfigureAwait(false);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<TResponse>(responseContent);
                return result ?? throw new InvalidOperationException("Failed to deserialize response");
            }

            var error = this.HandleErrorResponse(response.StatusCode, responseContent);
            throw new OpenAIException(error);
        }

        private async Task<TResponse> ExecutePostRequestAsync<TRequest, TResponse>(
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
                throw new OpenAIException(error);
            }

            // For rate limit or server errors, throw to trigger retry
            throw new OpenAIException(error);
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

        private static int GetRetryDelay(int attempt)
        {
            // Exponential backoff: 1s, 2s, 4s
            return (int)Math.Pow(2, attempt - 1) * 1000;
        }
    }
}
