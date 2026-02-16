// <copyright file="UserStatus.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.Domain.Aggregates;

/// <summary>
/// Represents the status of a user.
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// The user is active.
    /// </summary>
    Active = 0,

    /// <summary>
    /// The user is suspended.
    /// </summary>
    Suspended = 1,

    /// <summary>
    /// The user is deleted.
    /// </summary>
    Deleted = 2,
}
