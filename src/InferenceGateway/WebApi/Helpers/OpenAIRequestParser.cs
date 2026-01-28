using Microsoft.AspNetCore.Http;
using Synaxis.InferenceGateway.Application.Translation;
using System.Text.Json;

namespace Synaxis.InferenceGateway.WebApi.Helpers;

public static class OpenAIRequestParser
{
    public static async Task<OpenAIRequest?> ParseAsync(HttpContext? context, CancellationToken cancellationToken = default)
    {
        if (context == null) return null;

        try
        {
            context.Request.EnableBuffering();
            context.Request.Body.Position = 0; // Rewind just in case
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync(cancellationToken);
            context.Request.Body.Position = 0; // Reset for subsequent middleware

            if (string.IsNullOrWhiteSpace(body)) return null;

            return JsonSerializer.Deserialize<OpenAIRequest>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            // If the body is invalid JSON for an OpenAI request, throw a BadHttpRequestException
            // so middleware can convert it to a 400. Do not swallow other exceptions.
            throw new BadHttpRequestException("Invalid JSON body", ex);
        }
    }
}
