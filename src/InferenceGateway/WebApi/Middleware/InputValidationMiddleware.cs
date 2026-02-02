namespace Synaxis.InferenceGateway.WebApi.Middleware;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;

/// <summary>
/// Middleware for input validation at API boundary.
/// Validates request body size and content type.
/// </summary>
public class InputValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<InputValidationMiddleware> _logger;
    private readonly long _maxRequestBodySize = 10 * 1024 * 1024; // 10MB

    /// <summary>
    /// Initializes a new instance of the <see cref="InputValidationMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger for recording validation events.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public InputValidationMiddleware(
        RequestDelegate next,
        ILogger<InputValidationMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Invokes the middleware to process the HTTP request.
    /// Validates request body size and content type before passing to the next middleware.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.ContentLength > _maxRequestBodySize)
        {
            _logger.LogWarning("Request body too large: {ContentLength} bytes (max: {MaxSize} bytes)",
                context.Request.ContentLength, _maxRequestBodySize);

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = "Request body too large" });
            return;
        }

        if (context.Request.Method == HttpMethods.Post || context.Request.Method == HttpMethods.Put)
        {
            var contentType = context.Request.ContentType;
            if (string.IsNullOrEmpty(contentType) || !contentType.Contains("application/json"))
            {
                _logger.LogWarning("Invalid content type: {ContentType}", contentType);

                context.Response.StatusCode = (int)HttpStatusCode.UnsupportedMediaType;
                await context.Response.WriteAsJsonAsync(new { error = "Content-Type must be application/json" });
                return;
            }
        }

        await _next(context);
    }
}
