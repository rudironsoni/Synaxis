// <copyright file="RequestIdMiddleware.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Api.Middleware;

/// <summary>
/// Middleware that ensures each request has a unique request ID.
/// </summary>
public class RequestIdMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<RequestIdMiddleware> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestIdMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger instance.</param>
    public RequestIdMiddleware(RequestDelegate next, ILogger<RequestIdMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = context.Request.Headers["X-Request-ID"].FirstOrDefault()
                        ?? Guid.NewGuid().ToString("N");

        context.Items["RequestId"] = requestId;
        context.Response.Headers["X-Request-ID"] = requestId;

        using (this.logger.BeginScope(new Dictionary<string, object> { ["RequestId"] = requestId }))
        {
            await this.next(context).ConfigureAwait(false);
        }
    }
}
