// <copyright file="TenantRoutingScorePolicy.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities
{
    /// <summary>
    /// Tenant-level routing score configuration overrides.
    /// Merges with global config; only specified fields override.
    /// </summary>
    /// <param name="id">The policy ID.</param>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="overrides">The policy overrides.</param>
    public record TenantRoutingScorePolicy(
        string id,
        string tenantId,
        RoutingScorePolicyOverrides overrides)
        : RoutingScorePolicyBase(id, "Tenant", tenantId);
}
