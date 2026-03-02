// <copyright file="OpenAIErrorHandlerMiddleware.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Middleware
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Synaxis.InferenceGateway.WebApi.Errors;

    /// <summary>
    /// Middleware that handles exceptions and converts them to OpenAI-compatible error responses.
    /// </summary>
    public class OpenAIErrorHandlerMiddleware
    {
        private const int ClientSummaryLimit = 100;
        private readonly RequestDelegate _next;
        private readonly ILogger<OpenAIErrorHandlerMiddleware> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenAIErrorHandlerMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware delegate.</param>
        /// <param name="logger">The logger instance.</param>
        public OpenAIErrorHandlerMiddleware(RequestDelegate next, ILogger<OpenAIErrorHandlerMiddleware> logger)
        {
            this._next = next;
            this._logger = logger;
        }

        /// <summary>
        /// Invokes the middleware to handle exceptions.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await this._next(context).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (context.Response.HasStarted)
                {
                    throw;
                }

                var requestId = ResolveRequestId(context);
                var isStreamingRequest = IsStreamingRequest(context);

                this.LogUnhandledException(context, ex, requestId, isStreamingRequest);

                if (ex is AggregateException agg)
                {
                    await this.HandleAggregateExceptionAsync(context, agg, requestId, isStreamingRequest).ConfigureAwait(false);
                    return;
                }

                var (errorCode, errorType, statusCode) = ResolveErrorCode(ex);

                this._logger.LogError(
                    ex,
                    "Exception caught. RequestId: {RequestId}, Path: {Path}, ErrorCode: {ErrorCode}, ErrorType: {ErrorType}, ExceptionType: {ExceptionType}, Message: {ExceptionMessage}",
                    requestId,
                    context.Request.Path,
                    errorCode,
                    errorType,
                    ex.GetType().Name,
                    ex.Message);

                context.Response.StatusCode = statusCode;
                await WriteSingleErrorAsync(context, isStreamingRequest, requestId, errorType, errorCode, ex.Message).ConfigureAwait(false);
            }
        }

        private static bool IsStreamingRequest(HttpContext context)
        {
            return context.Request.Headers["Accept"].ToString().Contains("text/event-stream")
                   || context.Request.Query.ContainsKey("stream");
        }

        private static bool IsClientErrorException(Exception ex)
        {
            return ex is BadHttpRequestException || ex.InnerException is JsonException;
        }

        private async Task HandleAggregateExceptionAsync(
            HttpContext context,
            AggregateException agg,
            string requestId,
            bool isStreamingRequest)
        {
            var flat = agg.Flatten();
            var (details, summaries, statusCodes) = BuildAggregateDetails(flat.InnerExceptions);

            this._logger.LogError(
                agg,
                "AggregateException caught. RequestId: {RequestId}, Path: {Path}, Inner exceptions: {InnerExceptionCount}",
                requestId,
                context.Request.Path,
                flat.InnerExceptions.Count);

            var overallStatus = DetermineAggregateStatus(statusCodes);
            var message = $"Routing failed. Details: {string.Join(", ", summaries)}";

            context.Response.StatusCode = overallStatus;

            if (isStreamingRequest)
            {
                context.Response.ContentType = "text/event-stream";
                var errorEvent = new
                {
                    error = new
                    {
                        message,
                        code = "upstream_routing_failure",
                        details,
                        request_id = requestId,
                    },
                };

                await context.Response.WriteAsync($"data: {JsonSerializer.Serialize(errorEvent)}\n\n").ConfigureAwait(false);
                await context.Response.WriteAsync("data: [DONE]\n\n").ConfigureAwait(false);
                return;
            }

            context.Response.ContentType = "application/json";
            var error = new
            {
                error = new
                {
                    message,
                    code = "upstream_routing_failure",
                    details,
                    request_id = requestId,
                },
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(error)).ConfigureAwait(false);
        }

        private static (List<object> Details, List<string> Summaries, List<int> StatusCodes) BuildAggregateDetails(
            IReadOnlyCollection<Exception> exceptions)
        {
            var details = new List<object>(exceptions.Count);
            var summaries = new List<string>(exceptions.Count);
            var statusCodes = new List<int>(exceptions.Count);

            foreach (var inner in exceptions)
            {
                var (provider, status, summary) = DescribeAggregateFailure(inner);
                statusCodes.Add(status);
                summaries.Add(summary);
                details.Add(new
                {
                    provider,
                    message = inner.Message,
                    status,
                });
            }

            return (details, summaries, statusCodes);
        }

        private static string ResolveRequestId(HttpContext context)
        {
            return context.Request.Headers["X-Request-ID"].FirstOrDefault()
                ?? context.Request.Headers["x-request-id"].FirstOrDefault()
                ?? context.TraceIdentifier;
        }

        private void LogUnhandledException(HttpContext context, Exception ex, string requestId, bool isStreamingRequest)
        {
            var isClientError = IsClientErrorException(ex);

            if (isClientError)
            {
                this._logger.LogInformation(
                    "Client request validation failed. RequestId: {RequestId}, Path: {Path}, Method: {Method}, Error: {ErrorMessage}",
                    requestId,
                    context.Request.Path,
                    context.Request.Method,
                    ex.Message);
                return;
            }

            this._logger.LogError(
                ex,
                "Unhandled exception caught. RequestId: {RequestId}, Path: {Path}, Method: {Method}, IsStreaming: {IsStreaming}",
                requestId,
                context.Request.Path,
                context.Request.Method,
                isStreamingRequest);
        }

        private static (string ErrorCode, string ErrorType, int StatusCode) ResolveErrorCode(Exception ex)
        {
            if (ex is ArgumentException or BadHttpRequestException)
            {
                var errorCode = ErrorCodes.InvalidValue;
                return (errorCode, ErrorCodeMappings.GetErrorType(errorCode), (int)ErrorCodeMappings.GetStatusCode(errorCode));
            }

            var fallbackCode = ErrorCodes.InternalError;
            return (fallbackCode, ErrorCodeMappings.GetErrorType(fallbackCode), (int)ErrorCodeMappings.GetStatusCode(fallbackCode));
        }

        private static Task WriteSingleErrorAsync(
            HttpContext context,
            bool isStreamingRequest,
            string requestId,
            string errorType,
            string errorCode,
            string message)
        {
            if (isStreamingRequest)
            {
                return WriteStreamingErrorAsync(context, requestId, errorType, errorCode, message);
            }

            return WriteJsonErrorAsync(context, requestId, errorType, errorCode, message);
        }

        private static async Task WriteStreamingErrorAsync(
            HttpContext context,
            string requestId,
            string errorType,
            string errorCode,
            string message)
        {
            context.Response.ContentType = "text/event-stream";

            var errorEvent = new
            {
                error = new
                {
                    message,
                    type = errorType,
                    param = (string?)null,
                    code = errorCode,
                    request_id = requestId,
                },
            };

            await context.Response.WriteAsync($"data: {JsonSerializer.Serialize(errorEvent)}\n\n").ConfigureAwait(false);
            await context.Response.WriteAsync("data: [DONE]\n\n").ConfigureAwait(false);
        }

        private static Task WriteJsonErrorAsync(
            HttpContext context,
            string requestId,
            string errorType,
            string errorCode,
            string message)
        {
            context.Response.ContentType = "application/json";

            var singleError = new
            {
                error = new
                {
                    message,
                    type = errorType,
                    param = (string?)null,
                    code = errorCode,
                    request_id = requestId,
                },
            };

            return context.Response.WriteAsJsonAsync(singleError);
        }

        private static (string Provider, int Status, string Summary) DescribeAggregateFailure(Exception inner)
        {
            var provider = ResolveProviderName(inner);
            var status = ResolveStatusCode(inner);
            var summary = BuildSummary(provider, status, inner.Message);
            return (provider, status, summary);
        }

        private static string ResolveProviderName(Exception inner)
        {
            if (inner.Data.Contains("ProviderName") && inner.Data["ProviderName"] is string p && !string.IsNullOrEmpty(p))
            {
                return p;
            }

            if (!string.IsNullOrEmpty(inner.Source))
            {
                return inner.Source;
            }

            return inner.GetType().Name;
        }

        private static int ResolveStatusCode(Exception inner)
        {
            if (inner is HttpRequestException hre && hre.StatusCode.HasValue)
            {
                return (int)hre.StatusCode.Value;
            }

            var statusProp = inner.GetType().GetProperties()
                .FirstOrDefault(p => string.Equals(p.Name, "StatusCode", StringComparison.OrdinalIgnoreCase)
                                     && (p.PropertyType == typeof(int)
                                         || p.PropertyType == typeof(HttpStatusCode)
                                         || p.PropertyType == typeof(System.Net.HttpStatusCode)));
            if (statusProp != null)
            {
                try
                {
                    var val = statusProp.GetValue(inner);
                    if (val is int i)
                    {
                        return i;
                    }

                    if (val is HttpStatusCode hc)
                    {
                        return (int)hc;
                    }

                    if (val is System.Net.HttpStatusCode hcc)
                    {
                        return (int)hcc;
                    }
                }
                catch
                {
                    // ignore reflection failures and keep default
                }
            }

            return 500;
        }

        private static string BuildSummary(string provider, int status, string message)
        {
            var cleanMsg = message?.Replace("\r", string.Empty)?.Replace("\n", " ") ?? "Unknown error";
            if (cleanMsg.Length > ClientSummaryLimit)
            {
                cleanMsg = cleanMsg.Substring(0, ClientSummaryLimit - 3) + "...";
            }

            return $"[{provider}: {status} - {cleanMsg}]";
        }

        private static int DetermineAggregateStatus(IEnumerable<int> statusCodes)
        {
            var statusList = statusCodes.ToList();
            var anyRetriable = statusList.Any(sc => sc == 429 || (sc >= 500 && sc < 600));
            var allClientErrors = statusList.Count > 0 && statusList.All(sc => sc == 400 || sc == 404);

            if (anyRetriable)
            {
                return (int)HttpStatusCode.BadGateway; // 502
            }

            if (allClientErrors)
            {
                return (int)HttpStatusCode.BadRequest; // 400
            }

            return (int)HttpStatusCode.InternalServerError;
        }
    }
}
