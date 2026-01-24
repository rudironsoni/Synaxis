using Mediator;
using Synaplexer.Application.Dtos;

namespace Synaplexer.Application.Commands;

public record ChatCompletionCommand(
    string Model,
    ChatMessage[] Messages,
    float Temperature = 0.7f,
    int MaxTokens = 2048,
    bool Stream = false
) : ICommand<ChatCompletionResult>;
