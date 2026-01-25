using Microsoft.AspNetCore.Http;
using System;
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

            var statusCode = ex is ArgumentException ? (int)HttpStatusCode.BadRequest : (int)HttpStatusCode.InternalServerError;
            var type = ex is ArgumentException ? "invalid_request_error" : "server_error";
            var code = ex is ArgumentException ? "invalid_value" : "internal_error";

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var error = new
            {
                error = new
                {
                    message = ex.Message,
                    type = type,
                    param = (string?)null,
                    code = code
                }
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(error));
        }
    }
}
