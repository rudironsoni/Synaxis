namespace Synaplexer.Application.Dtos;

public record ChatCompletionResult(string Content, string Model, long UsageTokens, string? FinishReason = null);
