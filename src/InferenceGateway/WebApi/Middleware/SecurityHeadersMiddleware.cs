using Microsoft.AspNetCore.Http;

namespace Synaxis.InferenceGateway.WebApi.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly bool _isDevelopment;

    public SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment environment)
    {
        _next = next;
        _isDevelopment = environment.IsDevelopment();
    }

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
            }

            return Task.CompletedTask;
        });

        await _next(context);
    }
}
