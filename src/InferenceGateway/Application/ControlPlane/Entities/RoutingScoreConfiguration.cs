// <copyright file="RoutingScoreConfiguration.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities
{
    /// <summary>
    /// Configuration for routing score calculation.
    /// </summary>
    /// <param name="weights">The routing score weights.</param>
    /// <param name="freeTierBonusPoints">Bonus points for free providers (default 50).</param>
    /// <param name="minScoreThreshold">Minimum score threshold (default 0.0).</param>
    /// <param name="preferFreeByDefault">Whether to prefer free providers by default (default true).</param>
    public record RoutingScoreConfiguration(
        RoutingScoreWeights weights,
        int freeTierBonusPoints = 50,
        double minScoreThreshold = 0.0,
        bool preferFreeByDefault = true);
}
