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
                await this._next(context);
            }
            catch (Exception ex)
            {
                if (context.Response.HasStarted)
                {
                    throw;
                }

                var requestId = context.Request.Headers["X-Request-ID"].FirstOrDefault()
                             ?? context.Request.Headers["x-request-id"].FirstOrDefault()
                             ?? context.TraceIdentifier;

                var isStreamingRequest = IsStreamingRequest(context);

                this._logger.LogError(ex, "Unhandled exception caught. RequestId: {RequestId}, Path: {Path}, Method: {Method}, IsStreaming: {IsStreaming}",
                    requestId, context.Request.Path, context.Request.Method, isStreamingRequest);

                // Special handling for AggregateException produced by the router
                if (ex is AggregateException agg)
                {
                    var flat = agg.Flatten();
                    var details = new List<object>();
                    var summaries = new List<string>();
                    var statusCodes = new List<int>();

                    this._logger.LogError(ex, "AggregateException caught. RequestId: {RequestId}, Path: {Path}, Inner exceptions: {InnerExceptionCount}",
                        requestId, context.Request.Path, flat.InnerExceptions.Count);

                    foreach (var inner in flat.InnerExceptions)
                    {
                        // Provider name: try Data["ProviderName"], then Source, then exception type name
                        string provider = inner.Data.Contains("ProviderName") && inner.Data["ProviderName"] is string p && !string.IsNullOrEmpty(p)
                            ? p
                            : (!string.IsNullOrEmpty(inner.Source) ? inner.Source : inner.GetType().Name);

                        // Try to extract a status code from common exception types or reflection as fallback
                        int status = 500;
                        if (inner is HttpRequestException hre && hre.StatusCode.HasValue)
                        {
                            status = (int)hre.StatusCode.Value;
                        }
                        else
                        {
                            // Use robust reflection to avoid AmbiguousMatchException when multiple
                            // properties with the same name are present (explicit interface impls, etc).
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
                                        status = i;
                                    }
                                    else if (val is HttpStatusCode hc)
                                    {
                                        status = (int)hc;
                                    }
                                    else if (val is System.Net.HttpStatusCode hcc)
                                    {
                                        status = (int)hcc;
                                    }
                                }
                                catch
                                {
                                    // ignore reflection failures and keep default
                                }
                            }
                        }

                        statusCodes.Add(status);

                        // Clean up message: remove newlines for summary
                        var cleanMsg = inner.Message?.Replace("\r", "")?.Replace("\n", " ") ?? "Unknown error";
                        if (cleanMsg.Length > 100)
                        {
                            cleanMsg = cleanMsg.Substring(0, 97) + "...";
                        }

                        summaries.Add($"[{provider}: {status} - {cleanMsg}]");

                        details.Add(new
                        {
                            provider,
                            message = inner.Message,
                            status = status,
                        });
                    }

                    // Determine overall status code
                    int overallStatus;
                    bool anyRetriable = statusCodes.Any(sc => sc == 429 || (sc >= 500 && sc < 600));
                    bool allClientErrors400or404 = statusCodes.Count > 0 && statusCodes.All(sc => sc == 400 || sc == 404);

                    if (anyRetriable)
                    {
                        overallStatus = (int)HttpStatusCode.BadGateway; // 502
                    }
                    else if (allClientErrors400or404)
                    {
                        overallStatus = (int)HttpStatusCode.BadRequest; // 400
                    }
                    else
                    {
                        overallStatus = (int)HttpStatusCode.InternalServerError; // fallback
                    }

                    var message = $"Routing failed. Details: {string.Join(", ", summaries)}";

                    context.Response.StatusCode = overallStatus;

                    if (isStreamingRequest)
                    {
                        context.Response.ContentType = "text/event-stream";

                        var errorEvent = new
                        {
                            error = new
                            {
                                message = message,
                                code = "upstream_routing_failure",
                                details = details,
                                request_id = requestId
                            },
                        };

                        await context.Response.WriteAsync($"data: {JsonSerializer.Serialize(errorEvent)}\n\n");
                        await context.Response.WriteAsync("data: [DONE]\n\n");
                    }
                    else
                    {
                        context.Response.ContentType = "application/json";

                        var error = new
                        {
                            error = new
                            {
                                message = message,
                                code = "upstream_routing_failure",
                                details = details,
                                request_id = requestId
                            },
                        };

                        await context.Response.WriteAsync(JsonSerializer.Serialize(error));
                    }

                    return;
                }

                // Non-aggregate exceptions: use error code catalog
                string errorCode;
                string errorType;
                int statusCode;

                if (ex is ArgumentException or BadHttpRequestException)
                {
                    errorCode = ErrorCodes.InvalidValue;
                    errorType = ErrorCodeMappings.GetErrorType(errorCode);
                    statusCode = (int)ErrorCodeMappings.GetStatusCode(errorCode);
                }
                else
                {
                    errorCode = ErrorCodes.InternalError;
                    errorType = ErrorCodeMappings.GetErrorType(errorCode);
                    statusCode = (int)ErrorCodeMappings.GetStatusCode(errorCode);
                }

                this._logger.LogError(ex, "Exception caught. RequestId: {RequestId}, Path: {Path}, ErrorCode: {ErrorCode}, ErrorType: {ErrorType}, ExceptionType: {ExceptionType}, Message: {ExceptionMessage}",
                    requestId, context.Request.Path, errorCode, errorType, ex.GetType().Name, ex.Message);

                context.Response.StatusCode = statusCode;

                if (isStreamingRequest)
                {
                    context.Response.ContentType = "text/event-stream";

                    var errorEvent = new
                    {
                        error = new
                        {
                            message = ex.Message,
                            type = errorType,
                            param = (string?)null,
                            code = errorCode,
                            request_id = requestId
                        },
                    };

                    await context.Response.WriteAsync($"data: {JsonSerializer.Serialize(errorEvent)}\n\n");
                    await context.Response.WriteAsync("data: [DONE]\n\n");
                }
                else
                {
                    context.Response.ContentType = "application/json";

                    var singleError = new
                    {
                        error = new
                        {
                            message = ex.Message,
                            type = errorType,
                            param = (string?)null,
                            code = errorCode,
                            request_id = requestId
                        },
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(singleError));
                }
            }
        }

        private static bool IsStreamingRequest(HttpContext context)
        {
            return context.Request.Headers["Accept"].ToString().Contains("text/event-stream")
                   || context.Request.Query.ContainsKey("stream");
        }
    }
}
