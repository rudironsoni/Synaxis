using Synaplexer.Domain.ValueObjects;

namespace Synaplexer.Domain.Interfaces;

public interface ILlmProvider
{
    string Id { get; }
    string Name { get; }
    int Priority { get; }
    ProviderTier Tier { get; }
    bool SupportsModel(string modelId);
    Task<ChatCompletionResult> ChatAsync(ChatRequest request, CancellationToken ct = default);
    IAsyncEnumerable<ChatCompletionChunk> StreamChatAsync(ChatRequest request, CancellationToken ct = default);
}
