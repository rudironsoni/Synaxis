using ContextSavvy.LlmProviders.Domain.ValueObjects;

namespace ContextSavvy.LlmProviders.Domain.Interfaces;

public interface ILlmProvider
{
    string Id { get; }
    string Name { get; }
    ProviderTier Tier { get; }
    bool SupportsModel(string modelId);
    Task<ChatCompletionResult> ChatAsync(ChatRequest request, CancellationToken ct = default);
    IAsyncEnumerable<ChatCompletionChunk> StreamChatAsync(ChatRequest request, CancellationToken ct = default);
}
