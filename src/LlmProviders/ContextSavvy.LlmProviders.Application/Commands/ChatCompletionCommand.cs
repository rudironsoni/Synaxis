using Mediator;
using ContextSavvy.LlmProviders.Application.Dtos;

namespace ContextSavvy.LlmProviders.Application.Commands;

public record ChatCompletionCommand(
    string Model,
    ChatMessage[] Messages,
    float Temperature = 0.7f,
    int MaxTokens = 2048,
    bool Stream = false
) : ICommand<ChatCompletionResult>;
