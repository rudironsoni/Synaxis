namespace Synaxis.InferenceGateway.Application.Tests.Optimization.Caching;

/// <summary>
/// Interface for conversation compression
/// </summary>
public interface IConversationCompressor
{
    Task<CompressionResult> CompressAsync(
        List<ConversationMessage> messages,
        int tokenThreshold,
        CompressionStrategy strategy,
        CancellationToken cancellationToken);

    int EstimateTokens(string text);
}
