using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Synaxis.InferenceGateway.WebApi.Middleware;

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
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _isDevelopment = environment.IsDevelopment();
    }

    /// <summary>
    /// Processes the HTTP request and adds security headers to the response.
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
                response.Headers.Append("X-Content-Type-Options", "nosniff");
                response.Headers.Append("X-Frame-Options", "DENY");
                response.Headers.Append("X-XSS-Protection", "1; mode=block");
                response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
                response.Headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");

                if (!_isDevelopment)
                {
                    response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");
                }

                response.Headers.Append("Content-Security-Policy",
                    "default-src 'self'; " +
                    "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                    "style-src 'self' 'unsafe-inline'; " +
                    "img-src 'self' data: https:; " +
                    "font-src 'self' data:; " +
                    "connect-src 'self'; " +
                    "frame-ancestors 'none'; " +
                    "base-uri 'self'; " +
                    "form-action 'self';");

                _logger.LogDebug("Security headers added to response for {Path}", context.Request.Path);
            }

            return Task.CompletedTask;
        });

        await _next(context);
    }
}
