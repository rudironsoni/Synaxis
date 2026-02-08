namespace Synaxis.InferenceGateway.Application.Tests.Optimization.Caching;

/// <summary>
/// Represents the result of a cache lookup operation
/// </summary>
public class CacheResult
{
    public bool IsHit { get; set; }

    public string? Response { get; set; }

    public double SimilarityScore { get; set; }

    public DateTimeOffset CachedAt { get; set; }

    public float[]? QueryEmbedding { get; set; }
}
