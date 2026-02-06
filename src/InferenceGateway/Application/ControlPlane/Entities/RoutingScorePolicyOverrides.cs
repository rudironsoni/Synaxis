// <copyright file="RoutingScorePolicyOverrides.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities
{
    /// <summary>
    /// Overrides for routing score configuration.
    /// Nullable properties allow partial overrides (inherit from parent).
    /// </summary>
    /// <param name="weights">The routing score weights.</param>
    /// <param name="freeTierBonusPoints">The bonus points for free tier providers.</param>
    /// <param name="minScoreThreshold">The minimum score threshold.</param>
    /// <param name="preferFreeByDefault">Whether to prefer free providers by default.</param>
    public record RoutingScorePolicyOverrides(
        RoutingScoreWeights? weights = null,
        int? freeTierBonusPoints = null,
        double? minScoreThreshold = null,
        bool? preferFreeByDefault = null);
}
