// <copyright file="RoutingScorePolicyOverrides.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities
{
    /// <summary>
    /// Overrides for routing score configuration.
    /// Nullable properties allow partial overrides (inherit from parent).
    /// </summary>
    /// <param name="Weights">The routing score weights.</param>
    /// <param name="FreeTierBonusPoints">The bonus points for free tier providers.</param>
    /// <param name="MinScoreThreshold">The minimum score threshold.</param>
    /// <param name="PreferFreeByDefault">Whether to prefer free providers by default.</param>
    public record RoutingScorePolicyOverrides(
        RoutingScoreWeights? Weights = null,
        int? FreeTierBonusPoints = null,
        double? MinScoreThreshold = null,
        bool? PreferFreeByDefault = null);
}
