// <copyright file="ApiKey.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Kernel.Infrastructure.ApiManagement.Models;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents an API key in the external API Management platform.
/// </summary>
public sealed record ApiKey
{
    /// <summary>
    /// Gets the unique identifier of the key in the external system.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the actual API key value (only available on creation).
    /// </summary>
    public string? KeyValue { get; init; }

    /// <summary>
    /// Gets the display name of the key.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Gets the subscription ID associated with this key.
    /// </summary>
    public string? SubscriptionId { get; init; }

    /// <summary>
    /// Gets the state of the key.
    /// </summary>
    public ApiKeyState State { get; init; } = ApiKeyState.Active;

    /// <summary>
    /// Gets the creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the expiration timestamp.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Gets the last usage timestamp.
    /// </summary>
    public DateTimeOffset? LastUsedAt { get; init; }

    /// <summary>
    /// Gets the scopes or products associated with this key.
    /// </summary>
    public IReadOnlyList<string> Scopes { get; init; } = new List<string>();

    /// <summary>
    /// Gets custom metadata associated with the key.
    /// </summary>
    public IDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets the primary key (for Azure APIM).
    /// </summary>
    public string? PrimaryKey { get; init; }

    /// <summary>
    /// Gets the secondary key (for Azure APIM).
    /// </summary>
    public string? SecondaryKey { get; init; }
}
