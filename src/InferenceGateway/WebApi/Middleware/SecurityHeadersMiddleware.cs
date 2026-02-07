// <copyright file="SecurityHeadersMiddleware.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Middleware
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Middleware for adding security headers to all responses.
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SecurityHeadersMiddleware> _logger;
        private readonly bool _isDevelopment;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityHeadersMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="environment">The web hosting environment.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="next"/> or <paramref name="logger"/> is null.</exception>
        public SecurityHeadersMiddleware(
            RequestDelegate next,
            ILogger<SecurityHeadersMiddleware> logger,
            IWebHostEnvironment environment)
        {
            this._next = next ?? throw new ArgumentNullException(nameof(next));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._isDevelopment = environment.IsDevelopment();
        }

        /// <summary>
        /// Processes the HTTP request and adds comprehensive security headers to the response.
        /// Headers include CSP, HSTS (production), X-Frame-Options, and other protective measures.
        /// </summary>
        /// <param name="context">The HTTP context for the current request.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            var response = context.Response;

            response.OnStarting(() =>
            {
                if (!response.HasStarted)
                {
                    // Prevent MIME type sniffing
                    response.Headers.Append("X-Content-Type-Options", "nosniff");

                    // Prevent clickjacking attacks
                    response.Headers.Append("X-Frame-Options", "DENY");

                    // Enable XSS filtering
                    response.Headers.Append("X-XSS-Protection", "1; mode=block");

                    // Control referrer information
                    response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

                    // Restrict browser features
                    response.Headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=(), payment=(), usb=()");

                    // HSTS: Force HTTPS in production
                    if (!this._isDevelopment)
                    {
                        response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");
                    }

                    // Content Security Policy (CSP)
                    var csp = BuildContentSecurityPolicy(this._isDevelopment);
                    response.Headers.Append("Content-Security-Policy", csp);

                    // Remove server identification header for security through obscurity
                    response.Headers.Remove("Server");
                    response.Headers.Remove("X-Powered-By");

                    this._logger.LogDebug("Security headers added to response for {Path}", context.Request.Path);
                }

                return Task.CompletedTask;
            });

            await this._next(context);
        }

        /// <summary>
        /// Builds a Content Security Policy based on environment.
        /// Production uses stricter policies than development.
        /// </summary>
        private static string BuildContentSecurityPolicy(bool isDevelopment)
        {
            if (isDevelopment)
            {
                // More relaxed CSP for development (allows Scalar API docs, etc.)
                return "default-src 'self'; " +
                       "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                       "style-src 'self' 'unsafe-inline'; " +
                       "img-src 'self' data: https:; " +
                       "font-src 'self' data:; " +
                       "connect-src 'self' ws: wss:; " +
                       "frame-ancestors 'none'; " +
                       "base-uri 'self'; " +
                       "form-action 'self';";
            }

            // Stricter CSP for production
            return "default-src 'self'; " +
                   "script-src 'self'; " +
                   "style-src 'self'; " +
                   "img-src 'self' data:; " +
                   "font-src 'self'; " +
                   "connect-src 'self' wss:; " +
                   "frame-ancestors 'none'; " +
                   "base-uri 'self'; " +
                   "form-action 'self'; " +
                   "upgrade-insecure-requests;";
        }
    }
}
