namespace Synaxis.InferenceGateway.Application.Tests.Optimization.Configuration;

public class UserTokenOptimizationConfig
{
    public Guid UserId { get; set; }

    public double? SimilarityThreshold { get; set; }

    public int? CacheTtlSeconds { get; set; }

    public bool? EnableCaching { get; set; }
}
