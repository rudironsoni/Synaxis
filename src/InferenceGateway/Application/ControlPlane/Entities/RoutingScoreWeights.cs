// <copyright file="RoutingScoreWeights.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities
{
    /// <summary>
    /// Weighted scoring factors for intelligent routing.
    /// All weights must sum to approximately 1.0 for consistent scoring.
    /// </summary>
    /// <param name="qualityScoreWeight">Weight for quality (default 30%).</param>
    /// <param name="quotaRemainingWeight">Weight for remaining quota (default 30%).</param>
    /// <param name="rateLimitSafetyWeight">Weight for rate limit safety (default 20%).</param>
    /// <param name="latencyScoreWeight">Weight for latency (default 20%).</param>
    public record RoutingScoreWeights(
        double? qualityScoreWeight = 0.3,
        double? quotaRemainingWeight = 0.3,
        double? rateLimitSafetyWeight = 0.2,
        double? latencyScoreWeight = 0.2);
}
