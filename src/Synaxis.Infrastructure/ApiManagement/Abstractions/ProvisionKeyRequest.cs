// <copyright file="ProvisionKeyRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.ApiManagement.Abstractions;

using System.Collections.Generic;

/// <summary>
/// Request to provision a new API key.
/// </summary>
public sealed record ProvisionKeyRequest
{
    /// <summary>
    /// Gets the display name for the key.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Gets the scope or product to associate with the key.
    /// </summary>
    public string? Scope { get; init; }

    /// <summary>
    /// Gets the list of scopes or products.
    /// </summary>
    public IReadOnlyList<string> Scopes { get; init; } = new List<string>();

    /// <summary>
    /// Gets the expiration date for the key.
    /// </summary>
    public System.DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Gets custom metadata for the key.
    /// </summary>
    public IDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}
