// <copyright file="RoutingScoreCalculator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Routing
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Synaxis.InferenceGateway.Application.Configuration;
    using Synaxis.InferenceGateway.Application.ControlPlane.Entities;

    /// <summary>
    /// Implementation of routing score calculator.
    /// </summary>
    public class RoutingScoreCalculator : IRoutingScoreCalculator
    {
        private readonly ILogger<RoutingScoreCalculator> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoutingScoreCalculator"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public RoutingScoreCalculator(
            ILogger<RoutingScoreCalculator> logger)
        {
            this.logger = logger;
        }

        /// <inheritdoc/>
        public async Task<RoutingScoreConfiguration> GetEffectiveConfigurationAsync(
            string? tenantId,
            string? userId,
            CancellationToken ct = default)
        {
            // Load from database when DbContext is available
            // For now, return default configuration
            await Task.CompletedTask.ConfigureAwait(false);

            var defaultWeights = new RoutingScoreWeights(
                QualityScoreWeight: 0.3,
                QuotaRemainingWeight: 0.3,
                RateLimitSafetyWeight: 0.2,
                LatencyScoreWeight: 0.2);

            return new RoutingScoreConfiguration(
                Weights: defaultWeights,
                FreeTierBonusPoints: 50,
                MinScoreThreshold: 0.0,
                PreferFreeByDefault: true);
        }

        /// <inheritdoc/>
        public async Task<double> CalculateScoreAsync(
            EnrichedCandidate candidate,
            string? tenantId,
            string? userId,
            CancellationToken ct = default)
        {
            // Get effective configuration (cached in real implementation)
            var config = await this.GetEffectiveConfigurationAsync(tenantId, userId, ct).ConfigureAwait(false);
            var weights = config.Weights;

            // Ensure weights are not null (use defaults if null)
            var qualityWeight = weights.QualityScoreWeight ?? 0.3;
            var quotaWeight = weights.QuotaRemainingWeight ?? 0.3;
            var rateLimitWeight = weights.RateLimitSafetyWeight ?? 0.2;
            var latencyWeight = weights.LatencyScoreWeight ?? 0.2;

            // 1. Calculate base score from weighted factors
            double qualityScore = RoutingScoreCalculator.NormalizeScore(candidate.Config.QualityScore, 1, 10);  // 0-1
            double quotaScore = candidate.Config.EstimatedQuotaRemaining / 100.0;  // 0-1

            // Rate limit safety: 1.0 = safe, 0.0 = at limit
            // inferred from health checks or config
            double rateLimitSafety = 1.0;  // Get from quota tracker

            // Latency score: faster = better
            double latencyScore = candidate.Config.AverageLatencyMs.HasValue
                ? RoutingScoreCalculator.NormalizeScore(candidate.Config.AverageLatencyMs.Value, 0, 5000, reverse: true)
                : 0.5;  // Unknown latency = average

            // Cost score: cheaper = better (normalize cost to 0-1, with lower cost being better)
            // Assume cost range is 0-0.1 per token for most models
            double costScore = 0.5;  // Default if no cost info
            if (candidate.Cost != null)
            {
                var costPerToken = (double)candidate.Cost.CostPerToken;
                costScore = RoutingScoreCalculator.NormalizeScore(costPerToken, 0, 0.1, reverse: true);
            }

            // Calculate weighted score (0-100)
            // Cost is factored in with low weight (0.1) when models are in same tier
            double weightedScore =
                (qualityScore * qualityWeight)
                + (quotaScore * quotaWeight)
                + (rateLimitSafety * rateLimitWeight)
                + (latencyScore * latencyWeight)
                + (costScore * 0.1);  // Cost factor for same-tier sorting
            weightedScore *= 100;

            // 2. Add free tier bonus if configured
            if (candidate.IsFree && config.PreferFreeByDefault)
            {
                weightedScore += config.FreeTierBonusPoints;
            }

            this.logger.LogDebug(
                "Score for {Provider}: {Score:F2} (Quality: {Quality:F2}, Quota: {Quota:F2}, Latency: {Latency:F2}, IsFree: {IsFree})",
                candidate.Config.Type,
                weightedScore,
                qualityScore * qualityWeight * 100,
                quotaScore * quotaWeight * 100,
                latencyScore * latencyWeight * 100,
                candidate.IsFree);

            return weightedScore;
        }

        private static double NormalizeScore(double value, double min, double max, bool reverse = false)
        {
            var normalized = (value - min) / (max - min);
            normalized = Math.Max(0, Math.Min(1, normalized));  // Clamp to 0-1
            return reverse ? 1 - normalized : normalized;
        }
    }
}
