using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using System.Text.Json;
using System.Text.Json.Serialization;
using Synaxis.InferenceGateway.WebApi.DTOs.OpenAi;

namespace Synaxis.InferenceGateway.WebApi.Controllers.v1;

/// <summary>
/// Controller for OpenAI-compatible chat completions.
/// </summary>
[ApiController]
[Route("v1/chat/completions")]
public class ChatCompletionsController : ControllerBase
{
    private readonly IChatClient _chatClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public ChatCompletionsController(IChatClient chatClient)
    {
        _chatClient = chatClient;
        _jsonOptions = new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// Creates a model response for the given chat conversation.
    /// </summary>
    /// <param name="request">The chat completion request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A chat completion response or a stream of chunks.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ChatCompletionRequest request, CancellationToken cancellationToken)
    {
        // Simple mapping for now
        var messages = request.Messages.Select(m => new ChatMessage(new ChatRole(m.Role), m.Content?.ToString() ?? "")).ToList();
        var options = new ChatOptions 
        { 
            ModelId = request.Model,
            Temperature = (float?)request.Temperature,
            MaxOutputTokens = request.MaxTokens,
            TopP = (float?)request.TopP
        };

        if (request.Stream)
        {
            Response.Headers.Append("Content-Type", "text/event-stream");
            
            await foreach (var update in _chatClient.GetStreamingResponseAsync(messages, options, cancellationToken))
            {
                List<object>? toolCalls = null;
                if (update.Contents.Any(c => c is FunctionCallContent))
                {
                    toolCalls = update.Contents
                        .OfType<FunctionCallContent>()
                        .Select(fc => new 
                        { 
                            id = fc.CallId, 
                            type = "function", 
                            function = new { name = fc.Name, arguments = fc.Arguments } 
                        })
                        .Cast<object>()
                        .ToList();
                }

                var chunk = new ChatCompletionChunk
                {
                    Id = update.ResponseId ?? Guid.NewGuid().ToString(),
                    Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Model = request.Model,
                    Choices = new List<ChatCompletionChunkChoice>
                    {
                        new ChatCompletionChunkChoice
                        {
                            Index = 0, // Assuming single choice
                            Delta = new ChatCompletionChunkDelta
                            {
                                Role = update.Role?.Value,
                                Content = update.Text,
                                ToolCalls = toolCalls
                            },
                            FinishReason = update.FinishReason?.ToString()
                        }
                    }
                };
                
                var json = JsonSerializer.Serialize(chunk, _jsonOptions);
                await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
            
            await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
            return new EmptyResult();
        }
        else
        {
            var result = await _chatClient.GetResponseAsync(messages, options, cancellationToken);
            
            var response = new ChatCompletionResponse
            {
                Id = result.ResponseId ?? Guid.NewGuid().ToString(),
                Created = result.CreatedAt?.ToUnixTimeSeconds() ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Model = request.Model,
                Choices = result.Messages.Select((m, i) => new ChatCompletionChoice
                {
                    Index = i,
                    Message = new ChatCompletionMessageDto
                    {
                        Role = m.Role.Value,
                        Content = m.Text,
                        ToolCalls = m.Contents.OfType<FunctionCallContent>().Any() 
                            ? m.Contents.OfType<FunctionCallContent>().Select(fc => (object)new { id = fc.CallId, type = "function", function = new { name = fc.Name, arguments = fc.Arguments } }).ToList()
                            : null
                    },
                    FinishReason = result.FinishReason?.ToString()
                }).ToList(),
                Usage = result.Usage != null ? new ChatCompletionUsage
                {
                    PromptTokens = (int)(result.Usage.InputTokenCount ?? 0),
                    CompletionTokens = (int)(result.Usage.OutputTokenCount ?? 0),
                    TotalTokens = (int)(result.Usage.TotalTokenCount ?? 0)
                } : null
            };
            
            return Ok(response);
        }
    }
}
