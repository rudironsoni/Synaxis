// <copyright file="UserRoutingScorePolicy.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities
{
    /// <summary>
    /// User-level routing score configuration overrides.
    /// Merges with tenant config (which merges with global).
    /// Only specified fields override.
    /// </summary>
    /// <param name="id">The policy ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="overrides">The policy overrides.</param>
    public record UserRoutingScorePolicy(
        string id,
        string userId,
        RoutingScorePolicyOverrides overrides)
        : RoutingScorePolicyBase(id, "User", userId);
}
