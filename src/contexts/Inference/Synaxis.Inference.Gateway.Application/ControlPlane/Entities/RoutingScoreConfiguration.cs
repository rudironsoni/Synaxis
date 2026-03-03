// <copyright file="RoutingScoreConfiguration.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities
{
    /// <summary>
    /// Configuration for routing score calculation.
    /// </summary>
    /// <param name="Weights">The routing score weights.</param>
    /// <param name="FreeTierBonusPoints">Bonus points for free providers (default 50).</param>
    /// <param name="MinScoreThreshold">Minimum score threshold (default 0.0).</param>
    /// <param name="PreferFreeByDefault">Whether to prefer free providers by default (default true).</param>
    public record RoutingScoreConfiguration(
        RoutingScoreWeights Weights,
        int FreeTierBonusPoints = 50,
        double MinScoreThreshold = 0.0,
        bool PreferFreeByDefault = true);
}
