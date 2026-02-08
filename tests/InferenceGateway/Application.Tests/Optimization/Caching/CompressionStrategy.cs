namespace Synaxis.InferenceGateway.Application.Tests.Optimization.Caching;

/// <summary>
/// Compression strategy options
/// </summary>
public enum CompressionStrategy
{
    None,
    TruncateOldest,
    Smart,
    Summarize,
}
