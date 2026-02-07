// <copyright file="RateLimitingMiddleware.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Middleware
{
    using System.Net;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Middleware for rate limiting requests.
    /// Implements sliding window rate limiting (100 requests per minute per IP).
    /// </summary>
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private readonly int _requestsPerMinute = 100;
        private readonly TimeSpan _windowSize = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Initializes a new instance of the <see cref="RateLimitingMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="cache">The memory cache for tracking request counts.</param>
        /// <param name="logger">The logger for recording rate limit events.</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
        public RateLimitingMiddleware(
            RequestDelegate next,
            IMemoryCache cache,
            ILogger<RateLimitingMiddleware> logger)
        {
            this._next = next ?? throw new ArgumentNullException(nameof(next));
            this._cache = cache ?? throw new ArgumentNullException(nameof(cache));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Invokes the middleware to process the HTTP request.
        /// </summary>
        /// <param name="context">The HTTP context for the current request.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            var clientIp = GetClientIpAddress(context);
            var cacheKey = $"ratelimit:{clientIp}";

            var now = DateTime.UtcNow;
            var windowStart = now.Add(-this._windowSize);

            var requestTimestamps = this._cache.GetOrCreate(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = this._windowSize;
                return new List<DateTime>();
            }) ?? new List<DateTime>();

            lock (requestTimestamps)
            {
                requestTimestamps.RemoveAll(t => t < windowStart);

                if (requestTimestamps.Count >= this._requestsPerMinute)
                {
                    this._logger.LogWarning("Rate limit exceeded for IP {ClientIp}: {Count} requests in last minute", clientIp, requestTimestamps.Count);

                    context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                    context.Response.Headers.Append("X-RateLimit-Limit", this._requestsPerMinute.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    context.Response.Headers.Append("X-RateLimit-Remaining", "0");
                    context.Response.Headers.Append("X-RateLimit-Reset", requestTimestamps[0].Add(this._windowSize).ToString("R", System.Globalization.CultureInfo.InvariantCulture));

                    return;
                }

                requestTimestamps.Add(now);
            }

            context.Response.OnStarting(() =>
            {
                lock (requestTimestamps)
                {
                    var remaining = this._requestsPerMinute - requestTimestamps.Count;
                    context.Response.Headers.Append("X-RateLimit-Limit", this._requestsPerMinute.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    context.Response.Headers.Append("X-RateLimit-Remaining", remaining.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    context.Response.Headers.Append("X-RateLimit-Reset", requestTimestamps[0].Add(this._windowSize).ToString("R", System.Globalization.CultureInfo.InvariantCulture));
                }

                return Task.CompletedTask;
            });

            await this._next(context).ConfigureAwait(false);
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
}
