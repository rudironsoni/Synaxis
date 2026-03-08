// <copyright file="ApiKeyState.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Kernel.Infrastructure.ApiManagement.Models;

/// <summary>
/// Represents the state of an API key.
/// </summary>
public enum ApiKeyState
{
    /// <summary>
    /// The key is active and can be used.
    /// </summary>
    Active,

    /// <summary>
    /// The key is expired.
    /// </summary>
    Expired,

    /// <summary>
    /// The key has been revoked.
    /// </summary>
    Revoked,

    /// <summary>
    /// The key is suspended.
    /// </summary>
    Suspended,

    /// <summary>
    /// The key is submitted but not yet active.
    /// </summary>
    Submitted,
}
