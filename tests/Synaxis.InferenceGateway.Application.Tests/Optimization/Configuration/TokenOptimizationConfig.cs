namespace Synaxis.InferenceGateway.Application.Tests.Optimization.Configuration;

public class TokenOptimizationConfig
{
    public double SimilarityThreshold { get; set; }

    public int? CacheTtlSeconds { get; set; }

    public string? CompressionStrategy { get; set; }

    public bool EnableCaching { get; set; }

    public bool EnableCompression { get; set; }

    public int? MaxConcurrentRequests { get; set; }

    public int? MaxTokensPerRequest { get; set; }
}
