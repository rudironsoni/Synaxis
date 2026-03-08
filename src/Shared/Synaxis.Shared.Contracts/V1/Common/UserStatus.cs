// <copyright file="UserStatus.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Contracts.V1.Common;

/// <summary>
/// Represents the status of a user in the system.
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// User account is pending activation.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// User account is active and can access the system.
    /// </summary>
    Active = 1,

    /// <summary>
    /// User account has been suspended.
    /// </summary>
    Suspended = 2,

    /// <summary>
    /// User account has been deactivated.
    /// </summary>
    Deactivated = 3,
}
