// <copyright file="IRoutingTool.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Agents.Tools
{
    /// <summary>
    /// Tool for managing routing decisions.
    /// </summary>
    public interface IRoutingTool
    {
        /// <summary>
        /// Switches the provider for a model.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="modelId">The model ID.</param>
        /// <param name="fromProvider">The current provider.</param>
        /// <param name="toProvider">The target provider.</param>
        /// <param name="reason">The reason for the switch.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>True if successful, false otherwise.</returns>
        Task<bool> SwitchProviderAsync(Guid organizationId, string modelId, string fromProvider, string toProvider, string reason, CancellationToken ct = default);

        /// <summary>
        /// Gets routing metrics for a model.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="modelId">The model ID.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The routing metrics.</returns>
        Task<RoutingMetrics> GetRoutingMetricsAsync(Guid organizationId, string modelId, CancellationToken ct = default);
    }

    /// <summary>
    /// Represents routing metrics.
    /// </summary>
    /// <param name="TotalRequests">The total number of requests.</param>
    /// <param name="ProviderDistribution">Distribution of requests across providers.</param>
    /// <param name="AverageCost">The average cost per request.</param>
    public record RoutingMetrics(int TotalRequests, IDictionary<string, int> ProviderDistribution, decimal AverageCost);
}
