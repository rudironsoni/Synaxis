// <copyright file="IRoutingScoreCalculator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Routing
{
    using System.Threading;
    using System.Threading.Tasks;
    using Synaxis.InferenceGateway.Application.Configuration;
    using Synaxis.InferenceGateway.Application.ControlPlane.Entities;

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
        /// <param name="candidate">The candidate to score.</param>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <returns>The calculated score.</returns>
        double CalculateScore(
            EnrichedCandidate candidate,
            string? tenantId,
            string? userId);

        /// <summary>
        /// Get effective routing score configuration for a specific user.
        /// Merges Global → Tenant → User precedence.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The effective routing configuration.</returns>
        Task<RoutingScoreConfiguration> GetEffectiveConfigurationAsync(
            string? tenantId,
            string? userId,
            CancellationToken ct = default);
    }
}
