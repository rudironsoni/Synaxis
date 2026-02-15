// <copyright file="IRouter.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Routing.SmartRouter;

using Synaxis.Domain.Common;

/// <summary>
/// Interface for routing requests to AI providers.
/// </summary>
public interface IRouter
{
    /// <summary>
    /// Routes a request to the optimal provider based on ML predictions and heuristics.
    /// </summary>
    /// <param name="request">The routing request.</param>
    /// <returns>A routing decision containing the selected provider and alternatives.</returns>
    Task<RoutingDecision> RouteRequestAsync(RoutingRequest request);

    /// <summary>
    /// Gets routing metrics for monitoring and analysis.
    /// </summary>
    /// <param name="timeWindow">The time window for metrics (default: last hour).</param>
    /// <returns>Routing metrics including latency, success rate, and provider utilization.</returns>
    Task<RoutingMetrics> GetRoutingMetricsAsync(TimeSpan? timeWindow = null);
}
