// <copyright file="UsageTrackingMiddleware.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Middleware
{
    using System.Diagnostics;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Middleware for tracking usage at request level.
    /// Ensures 100% accountability for all requests.
    /// </summary>
    public class UsageTrackingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<UsageTrackingMiddleware> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UsageTrackingMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">The logger for recording usage tracking events.</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
        public UsageTrackingMiddleware(
            RequestDelegate next,
            ILogger<UsageTrackingMiddleware> logger)
        {
            this._next = next ?? throw new ArgumentNullException(nameof(next));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Invokes the middleware to process the HTTP request and track usage data.
        /// </summary>
        /// <param name="context">The HTTP context for the current request.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var timestamp = DateTime.UtcNow;

            try
            {
                await this._next(context).ConfigureAwait(false);
            }
            finally
            {
                stopwatch.Stop();

                var usageData = new UsageData(
                    context.Request.Path,
                    context.Request.Method,
                    timestamp,
                    stopwatch.ElapsedMilliseconds,
                    context.Response.StatusCode,
                    GetClientIpAddress(context));

                context.Items["UsageData"] = usageData;

                this._logger.LogInformation(
                    "Request tracked: {Method} {Path} - {StatusCode} in {DurationMs}ms from {ClientIp}",
                    usageData.Method,
                    usageData.Path,
                    usageData.StatusCode,
                    usageData.DurationMs,
                    usageData.ClientIp);
            }
        }

        /// <summary>
        /// Gets the client IP address from the HTTP context.
        /// Checks X-Forwarded-For header first for proxied requests.
        /// </summary>
        /// <param name="context">The HTTP context for the current request.</param>
        /// <returns>The client IP address, or "unknown" if not available.</returns>
        private static string GetClientIpAddress(HttpContext context)
        {
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }

    /// <summary>
    /// Usage data for a single request.
    /// </summary>
    public record UsageData(
        string Path,
        string Method,
        DateTime Timestamp,
        long DurationMs,
        int StatusCode,
        string ClientIp);
}
