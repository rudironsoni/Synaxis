using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Synaxis.InferenceGateway.WebApi.Middleware;

public class OpenAIMetadataMiddleware
{
    private readonly RequestDelegate _next;

    public OpenAIMetadataMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            if (context.Items.TryGetValue("RoutingContext", out var value) && value is RoutingContext rc)
            {
                context.Response.Headers["x-gateway-model-requested"] = rc.RequestedModel;
                context.Response.Headers["x-gateway-model-resolved"] = rc.ResolvedCanonicalId;
                context.Response.Headers["x-gateway-provider"] = rc.Provider;
            }
            else
            {
                // Ensure headers are present even if empty
                context.Response.Headers["x-gateway-model-requested"] = "";
                context.Response.Headers["x-gateway-model-resolved"] = "";
                context.Response.Headers["x-gateway-provider"] = "";
            }
            
            if (!context.Response.Headers.ContainsKey("x-request-id"))
            {
                context.Response.Headers["x-request-id"] = context.TraceIdentifier;
            }
            
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
