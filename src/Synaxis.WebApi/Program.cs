using System.Text.Json;
using Microsoft.Extensions.AI;
using Synaxis.Application.Extensions;
using Synaxis.Application.Configuration;
using Synaxis.Infrastructure;
using Microsoft.Extensions.Options;

using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Services
builder.Services.AddHttpClient();
builder.Services.AddSynaxisApplication(builder.Configuration);
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapPost("/v1/chat/completions", async (HttpContext ctx, CreateChatCompletionRequest request, IChatClient brain) =>
{
    var options = new ChatOptions { ModelId = request.Model };
    var messages = request.Messages.Select(m => new ChatMessage(new ChatRole(m.Role), m.Content)).ToList();

    if (!request.Stream)
    {
        var response = await brain.GetResponseAsync(messages, options);
        return Results.Ok(new
        {
            id = Guid.NewGuid().ToString(),
            @object = "chat.completion",
            created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            model = response.ModelId ?? request.Model,
            choices = new[]
            {
                new
                {
                    index = 0,
                    message = new { role = "assistant", content = response.Text },
                    finish_reason = response.FinishReason?.ToString().ToLowerInvariant() ?? "stop"
                }
            },
            usage = new
            {
                prompt_tokens = response.Usage?.InputTokenCount ?? 0,
                completion_tokens = response.Usage?.OutputTokenCount ?? 0,
                total_tokens = (response.Usage?.InputTokenCount ?? 0) + (response.Usage?.OutputTokenCount ?? 0)
            }
        });
    }

    ctx.Response.Headers.ContentType = "text/event-stream";
    ctx.Response.Headers.CacheControl = "no-cache";
    ctx.Response.Headers.Connection = "keep-alive";

    var completionId = Guid.NewGuid().ToString();
    var created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    await foreach (var update in brain.GetStreamingResponseAsync(messages, options))
    {
        var chunk = new
        {
            id = completionId,
            @object = "chat.completion.chunk",
            created = created,
            model = update.ModelId ?? request.Model,
            choices = new[]
            {
                new
                {
                    index = 0,
                    delta = new { content = update.Text },
                    finish_reason = update.FinishReason?.ToString().ToLowerInvariant()
                }
            }
        };

        await ctx.Response.WriteAsync($"data: {JsonSerializer.Serialize(chunk)}\n\n");
        await ctx.Response.Body.FlushAsync();
    }

    await ctx.Response.WriteAsync("data: [DONE]\n\n");
    await ctx.Response.Body.FlushAsync();

    return Results.Empty;
});

app.Run();

public record CreateChatCompletionRequest(string Model, List<ChatMessageDto> Messages, bool Stream = false);
public record ChatMessageDto(string Role, string Content);

public partial class Program { }
