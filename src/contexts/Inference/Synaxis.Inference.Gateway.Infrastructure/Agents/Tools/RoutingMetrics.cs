// <copyright file="RoutingMetrics.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Agents.Tools
{
    /// <summary>
    /// Represents routing metrics.
    /// </summary>
    /// <param name="TotalRequests">The total number of requests.</param>
    /// <param name="ProviderDistribution">Distribution of requests across providers.</param>
    /// <param name="AverageCost">The average cost per request.</param>
    public record RoutingMetrics(int TotalRequests, IDictionary<string, int> ProviderDistribution, decimal AverageCost);
}