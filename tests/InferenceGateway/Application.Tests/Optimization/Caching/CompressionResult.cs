namespace Synaxis.InferenceGateway.Application.Tests.Optimization.Caching;

/// <summary>
/// Result of a compression operation
/// </summary>
public class CompressionResult
{
    public List<ConversationMessage> CompressedMessages { get; set; } = new();

    public int OriginalTokenCount { get; set; }

    public int CompressedTokenCount { get; set; }

    public double CompressionRatio { get; set; }

    public CompressionStrategy StrategyUsed { get; set; }

    public bool WasCompressed { get; set; }
}
