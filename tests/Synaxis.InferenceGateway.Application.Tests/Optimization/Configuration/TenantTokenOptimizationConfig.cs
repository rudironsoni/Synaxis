namespace Synaxis.InferenceGateway.Application.Tests.Optimization.Configuration;

// Mock entity classes for Token Optimization configuration testing
public class TenantTokenOptimizationConfig
{
    public Guid TenantId { get; set; }

    public double? SimilarityThreshold { get; set; }

    public int? CacheTtlSeconds { get; set; }

    public string? CompressionStrategy { get; set; }

    public bool? EnableCaching { get; set; }
}
