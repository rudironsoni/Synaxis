// <copyright file="GlobalRoutingScorePolicy.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities
{
    /// <summary>
    /// Global routing score configuration - system-wide default.
    /// All tenants inherit from this unless overridden.
    /// </summary>
    /// <param name="id">The policy ID.</param>
    /// <param name="configuration">The routing score configuration.</param>
    public record GlobalRoutingScorePolicy(
        string id,
        RoutingScoreConfiguration configuration)
        : RoutingScorePolicyBase(id, "Global", null);
}
