// <copyright file="TenantStatus.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.Domain.Aggregates;

/// <summary>
/// Represents the status of a tenant.
/// </summary>
public enum TenantStatus
{
    /// <summary>
    /// The tenant is active.
    /// </summary>
    Active = 0,

    /// <summary>
    /// The tenant is suspended.
    /// </summary>
    Suspended = 1,

    /// <summary>
    /// The tenant is deleted.
    /// </summary>
    Deleted = 2,
}
