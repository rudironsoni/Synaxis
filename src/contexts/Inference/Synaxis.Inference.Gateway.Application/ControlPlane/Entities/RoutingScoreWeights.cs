// <copyright file="RoutingScoreWeights.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities
{
    /// <summary>
    /// Weighted scoring factors for intelligent routing.
    /// All weights must sum to approximately 1.0 for consistent scoring.
    /// </summary>
    /// <param name="QualityScoreWeight">Weight for quality (default 30%).</param>
    /// <param name="QuotaRemainingWeight">Weight for remaining quota (default 30%).</param>
    /// <param name="RateLimitSafetyWeight">Weight for rate limit safety (default 20%).</param>
    /// <param name="LatencyScoreWeight">Weight for latency (default 20%).</param>
    public record RoutingScoreWeights(
        double? QualityScoreWeight = 0.3,
        double? QuotaRemainingWeight = 0.3,
        double? RateLimitSafetyWeight = 0.2,
        double? LatencyScoreWeight = 0.2);
}
