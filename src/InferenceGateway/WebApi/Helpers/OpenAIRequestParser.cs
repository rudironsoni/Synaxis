using Microsoft.AspNetCore.Http;
using Synaxis.InferenceGateway.Application.Translation;
using System.Text;
using System.Text.Json;

namespace Synaxis.InferenceGateway.WebApi.Helpers;

public static class OpenAIRequestParser
{
    public static async Task<OpenAIRequest?> ParseAsync(HttpContext? context, CancellationToken cancellationToken = default)
    {
        if (context == null) return null;

        const long MaxBodySize = 10L * 1024 * 1024; // 10 MB

        // If the client provided a Content-Length header, enforce it immediately.
        var contentLength = context.Request.ContentLength;
        if (contentLength.HasValue && contentLength.Value > MaxBodySize)
        {
            throw new BadHttpRequestException($"Request body too large. Limit is {MaxBodySize} bytes.");
        }

        // Enable buffering so we can read and then rewind for downstream middleware.
        context.Request.EnableBuffering();
        context.Request.Body.Position = 0; // Rewind just in case

        string body;

        if (contentLength.HasValue)
        {
            // Content-Length is known and already checked against the limit above.
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            body = await reader.ReadToEndAsync(cancellationToken);
        }
        else
        {
            // Content-Length is unknown. Read the stream in bounded chunks to avoid OOM/DoS.
            using var ms = new MemoryStream();
            var buffer = new byte[8192];
            int bytesRead;
            long totalRead = 0;

            while ((bytesRead = await context.Request.Body.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
            {
                totalRead += bytesRead;
                if (totalRead > MaxBodySize)
                {
                    // Reset position for downstream just in case, then reject.
                    context.Request.Body.Position = 0;
                    throw new BadHttpRequestException($"Request body too large. Limit is {MaxBodySize} bytes.");
                }

                ms.Write(buffer, 0, bytesRead);
            }

            ms.Position = 0;
            using var reader = new StreamReader(ms, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            body = await reader.ReadToEndAsync(cancellationToken);
        }

        // Reset the request body position so subsequent middleware can read it.
        context.Request.Body.Position = 0;

        if (string.IsNullOrWhiteSpace(body)) return null;

        try
        {
            return JsonSerializer.Deserialize<OpenAIRequest>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            // If the body is invalid JSON for an OpenAI request, throw a BadHttpRequestException
            // so middleware can convert it to a 400. Do not swallow other exceptions (OOM, etc.).
            throw new BadHttpRequestException("Invalid JSON body", ex);
        }
    }
}
