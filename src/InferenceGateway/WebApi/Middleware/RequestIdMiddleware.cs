using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace Synaxis.InferenceGateway.WebApi.Middleware;

public class RequestIdMiddleware
{
    private readonly RequestDelegate _next;

    public RequestIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = context.Request.Headers["X-Request-ID"].FirstOrDefault()
                     ?? context.Request.Headers["x-request-id"].FirstOrDefault()
                     ?? context.TraceIdentifier;

        context.Response.Headers["X-Request-ID"] = requestId;

        await _next(context);
    }
}
