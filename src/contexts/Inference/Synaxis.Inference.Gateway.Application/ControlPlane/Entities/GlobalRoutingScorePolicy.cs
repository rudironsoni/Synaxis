// <copyright file="GlobalRoutingScorePolicy.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities
{
    /// <summary>
    /// Global routing score configuration - system-wide default.
    /// All tenants inherit from this unless overridden.
    /// </summary>
    /// <param name="Id">The policy ID.</param>
    /// <param name="Configuration">The routing score configuration.</param>
    public record GlobalRoutingScorePolicy(
        string Id,
        RoutingScoreConfiguration Configuration)
        : RoutingScorePolicyBase(Id, "Global", null);
}
