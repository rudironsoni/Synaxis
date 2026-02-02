using Synaxis.InferenceGateway.Application.Configuration;
using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
using Microsoft.Extensions.Logging;

namespace Synaxis.InferenceGateway.Application.Routing;

/// <summary>
/// Calculates weighted routing scores for intelligent provider selection.
/// Implements 3-level precedence: Global → Tenant → User.
/// Scores consider quality, quota, rate limits, and latency.
/// </summary>
public interface IRoutingScoreCalculator
{
    /// <summary>
    /// Calculate routing score for a candidate provider.
    /// Score range: 0-100+ (can exceed 100 with free tier bonus).
    /// </summary>
    double CalculateScore(
        EnrichedCandidate candidate,
        string? tenantId,
        string? userId);
    
    /// <summary>
    /// Get effective routing score configuration for a specific user.
    /// Merges Global → Tenant → User precedence.
    /// </summary>
    Task<RoutingScoreConfiguration> GetEffectiveConfigurationAsync(
        string? tenantId,
        string? userId,
        CancellationToken ct = default);
}

public class RoutingScoreCalculator : IRoutingScoreCalculator
{
    private readonly ILogger<RoutingScoreCalculator> _logger;
    
    public RoutingScoreCalculator(
        ILogger<RoutingScoreCalculator> logger)
    {
        _logger = logger;
    }
    
    public async Task<RoutingScoreConfiguration> GetEffectiveConfigurationAsync(
        string? tenantId,
        string? userId,
        CancellationToken ct = default)
    {
        // TODO: Load from database when DbContext is available
        // For now, return default configuration
        await Task.CompletedTask;
        
        var defaultWeights = new RoutingScoreWeights(
            QualityScoreWeight: 0.3,
            QuotaRemainingWeight: 0.3,
            RateLimitSafetyWeight: 0.2,
            LatencyScoreWeight: 0.2
        );
        
        return new RoutingScoreConfiguration(
            Weights: defaultWeights,
            FreeTierBonusPoints: 50,
            MinScoreThreshold: 0.0,
            PreferFreeByDefault: true
        );
    }
    
    private RoutingScoreConfiguration MergeConfigurationOverrides(
        RoutingScoreConfiguration baseConfig,
        RoutingScorePolicyOverrides overrides)
    {
        var weights = baseConfig.Weights;
        
        // Merge weight overrides
        if (overrides.Weights != null)
        {
            weights = new RoutingScoreWeights(
                QualityScoreWeight: overrides.Weights.QualityScoreWeight ?? weights.QualityScoreWeight,
                QuotaRemainingWeight: overrides.Weights.QuotaRemainingWeight ?? weights.QuotaRemainingWeight,
                RateLimitSafetyWeight: overrides.Weights.RateLimitSafetyWeight ?? weights.RateLimitSafetyWeight,
                LatencyScoreWeight: overrides.Weights.LatencyScoreWeight ?? weights.LatencyScoreWeight
            );
        }
        
        return new RoutingScoreConfiguration(
            Weights: weights,
            FreeTierBonusPoints: overrides.FreeTierBonusPoints ?? baseConfig.FreeTierBonusPoints,
            MinScoreThreshold: overrides.MinScoreThreshold ?? baseConfig.MinScoreThreshold,
            PreferFreeByDefault: overrides.PreferFreeByDefault ?? baseConfig.PreferFreeByDefault
        );
    }
    
    public double CalculateScore(
        EnrichedCandidate candidate,
        string? tenantId,
        string? userId)
    {
        // Get effective configuration (cached in real implementation)
        var config = GetEffectiveConfigurationAsync(tenantId, userId).GetAwaiter().GetResult();
        var weights = config.Weights;

        // Ensure weights are not null (use defaults if null)
        var qualityWeight = weights.QualityScoreWeight ?? 0.3;
        var quotaWeight = weights.QuotaRemainingWeight ?? 0.3;
        var rateLimitWeight = weights.RateLimitSafetyWeight ?? 0.2;
        var latencyWeight = weights.LatencyScoreWeight ?? 0.2;

        // 1. Calculate base score from weighted factors
        double qualityScore = NormalizeScore(candidate.Config.QualityScore, 1, 10);  // 0-1
        double quotaScore = candidate.Config.EstimatedQuotaRemaining / 100.0;  // 0-1

        // Rate limit safety: 1.0 = safe, 0.0 = at limit
        // inferred from health checks or config
        double rateLimitSafety = 1.0;  // TODO: Get from quota tracker

        // Latency score: faster = better
        double latencyScore = candidate.Config.AverageLatencyMs.HasValue
            ? NormalizeScore(candidate.Config.AverageLatencyMs.Value, 0, 5000, reverse: true)
            : 0.5;  // Unknown latency = average

        // Calculate weighted score (0-100)
        double weightedScore =
            (qualityScore * qualityWeight +
             quotaScore * quotaWeight +
             rateLimitSafety * rateLimitWeight +
             latencyScore * latencyWeight) * 100;

        // 2. Add free tier bonus if configured
        if (candidate.IsFree && config.PreferFreeByDefault)
        {
            weightedScore += config.FreeTierBonusPoints;
        }

        _logger.LogDebug("Score for {Provider}: {Score:F2} (Quality: {Quality:F2}, Quota: {Quota:F2}, Latency: {Latency:F2}, IsFree: {IsFree})",
            candidate.Config.Type,
            weightedScore,
            qualityScore * qualityWeight * 100,
            quotaScore * quotaWeight * 100,
            latencyScore * latencyWeight * 100,
            candidate.IsFree);

        return weightedScore;
    }
    
    private double NormalizeScore(double value, double min, double max, bool reverse = false)
    {
        var normalized = (value - min) / (max - min);
        normalized = Math.Max(0, Math.Min(1, normalized));  // Clamp to 0-1
        return reverse ? 1 - normalized : normalized;
    }
}
