using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace Synaxis.InferenceGateway.WebApi.Middleware;

public class OpenAIErrorHandlerMiddleware
{
    private readonly RequestDelegate _next;

    public OpenAIErrorHandlerMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted) throw;

            // Special handling for AggregateException produced by the router
            if (ex is AggregateException agg)
            {
                var flat = agg.Flatten();
                var details = new List<object>();
                var summaries = new List<string>();
                var statusCodes = new List<int>();

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
                                if (val is int i) status = i;
                                else if (val is HttpStatusCode hc) status = (int)hc;
                                else if (val is System.Net.HttpStatusCode hcc) status = (int)hcc;
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
                    if (cleanMsg.Length > 100) cleanMsg = cleanMsg.Substring(0, 97) + "...";

                    summaries.Add($"[{provider}: {status} - {cleanMsg}]");

                    details.Add(new
                    {
                        provider,
                        message = inner.Message,
                        status = status
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
                context.Response.ContentType = "application/json";

                var error = new
                {
                    error = new
                    {
                        message = message,
                        code = "upstream_routing_failure",
                        details = details
                    }
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(error));
                return;
            }

            // Non-aggregate exceptions: keep previous behavior
            var statusCode = ex is ArgumentException ? (int)HttpStatusCode.BadRequest : (int)HttpStatusCode.InternalServerError;
            var type = ex is ArgumentException ? "invalid_request_error" : "server_error";
            var code = ex is ArgumentException ? "invalid_value" : "internal_error";

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var singleError = new
            {
                error = new
                {
                    message = ex.Message,
                    type = type,
                    param = (string?)null,
                    code = code
                }
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(singleError));
        }
    }
}
