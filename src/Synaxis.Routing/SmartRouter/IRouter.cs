// <copyright file="IRouter.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Routing.SmartRouter;

/// <summary>
/// Interface for routing requests to AI providers.
/// </summary>
public interface IRouter
{
    /// <summary>
    /// Routes a request to the optimal provider based on ML predictions and heuristics.
    /// </summary>
    /// <param name="request">The routing request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A routing decision containing the selected provider and alternatives.</returns>
    Task<RoutingDecision> RouteRequestAsync(RoutingRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets routing metrics for monitoring and analysis.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>Routing metrics including latency, success rate, and provider utilization.</returns>
    Task<RoutingMetrics> GetRoutingMetricsAsync(CancellationToken cancellationToken = default);
}
