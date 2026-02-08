// <copyright file="TenantRoutingScorePolicy.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities
{
    /// <summary>
    /// Tenant-level routing score configuration overrides.
    /// Merges with global config; only specified fields override.
    /// </summary>
    /// <param name="Id">The policy ID.</param>
    /// <param name="TenantId">The tenant ID.</param>
    /// <param name="Overrides">The policy overrides.</param>
    public record TenantRoutingScorePolicy(
        string Id,
        string TenantId,
        RoutingScorePolicyOverrides Overrides)
        : RoutingScorePolicyBase(Id, "Tenant", TenantId);
}
