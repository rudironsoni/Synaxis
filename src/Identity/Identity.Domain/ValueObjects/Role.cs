// <copyright file="Role.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.Domain.ValueObjects;

/// <summary>
/// Represents the role of a user within a team or tenant.
/// </summary>
public enum Role
{
    /// <summary>
    /// Owner has full control over the tenant or team.
    /// </summary>
    Owner = 0,

    /// <summary>
    /// Admin has administrative privileges but cannot delete the tenant or team.
    /// </summary>
    Admin = 1,

    /// <summary>
    /// Member has standard access to resources.
    /// </summary>
    Member = 2,

    /// <summary>
    /// Viewer has read-only access to resources.
    /// </summary>
    Viewer = 3,
}
