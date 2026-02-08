// <copyright file="RoutingScorePolicyBase.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities
{
    /// <summary>
    /// Base record for routing score configuration policies across the hierarchy.
    /// Supports 3-level precedence: Global → Tenant → User.
    /// Undefined fields inherit from parent level.
    /// </summary>
    /// <param name="Id">The policy ID.</param>
    /// <param name="OwnerType">The owner type: "Global", "Tenant", or "User".</param>
    /// <param name="OwnerId">The owner ID: null for Global, tenantId for Tenant, userId for User.</param>
    public abstract record RoutingScorePolicyBase(
        string Id,
        string OwnerType,
        string? OwnerId);
}
