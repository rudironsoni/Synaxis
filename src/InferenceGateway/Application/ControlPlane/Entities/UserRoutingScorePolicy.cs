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
    /// <param name="Id">The policy ID.</param>
    /// <param name="UserId">The user ID.</param>
    /// <param name="Overrides">The policy overrides.</param>
    public record UserRoutingScorePolicy(
        string Id,
        string UserId,
        RoutingScorePolicyOverrides Overrides)
        : RoutingScorePolicyBase(Id, "User", UserId);
}
