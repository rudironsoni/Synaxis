namespace Synaplexer.Domain.ValueObjects;

public record ChatRequest(
    string Model,
    List<Message> Messages,
    double Temperature = 0.7,
    int MaxTokens = 1000
);

public record Message(string Role, string Content);

public record ChatCompletionResult(
    string Id,
    string Content,
    string FinishReason,
    Usage Usage
);

public record ChatCompletionChunk(
    string Id,
    string Delta,
    string FinishReason
);

public record Usage(int PromptTokens, int CompletionTokens, int TotalTokens);
