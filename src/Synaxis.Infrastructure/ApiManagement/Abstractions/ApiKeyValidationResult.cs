// <copyright file="ApiKeyValidationResult.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.ApiManagement.Abstractions;

using System.Collections.Generic;

/// <summary>
/// Result of API key validation from external API Management.
/// </summary>
public sealed record ApiKeyValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the API key is valid.
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Gets the key ID from the external API Management platform.
    /// </summary>
    public string? KeyId { get; init; }

    /// <summary>
    /// Gets the subscription ID associated with the key.
    /// </summary>
    public string? SubscriptionId { get; init; }

    /// <summary>
    /// Gets the error message if validation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the scopes or products associated with the key.
    /// </summary>
    public IReadOnlyList<string> Scopes { get; init; } = new List<string>();

    /// <summary>
    /// Gets custom metadata associated with the key.
    /// </summary>
    public IDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}
