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
            // Log warning but return null to allow other endpoints/handlers to potentially handle if format mismatches?
            // Actually, if we are in this parser, we expect OpenAI request.
            // But legacy parser might just return null.
            // Better to log it.
            // Note: Parser is static, no logger injected. We can't log easily here without changing signature.
            // For now, rethrow or at least don't swallow silently if possible?
            // The audit said "Log exception details... or allow bubbling".
            // Since we can't log (static), bubbling is better than swallowing.
            // But the caller (RoutingAgent) might expect null.
            // Let's rely on the middleware to handle the exception if we throw.
            throw new BadHttpRequestException("Invalid JSON body", ex);
        }
        catch
        {
            return null;
        }
    }
}
