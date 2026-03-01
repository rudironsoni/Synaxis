// <copyright file="UserUpdated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.DomainEvents;

using Synaxis.Contracts.V1.Common;

/// <summary>
/// Event raised when a user is updated.
/// </summary>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(UserUpdated), "user_updated")]
public record UserUpdated : DomainEventBase
{
    /// <summary>
    /// Gets the updated email address (null if unchanged).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("email")]
    public string? Email { get; init; }

    /// <summary>
    /// Gets the updated display name (null if unchanged).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }

    /// <summary>
    /// Gets the updated status (null if unchanged).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public UserStatus? Status { get; init; }

    /// <summary>
    /// Gets the updated roles (null if unchanged).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("roles")]
    public IReadOnlyList<string>? Roles { get; init; }

    /// <summary>
    /// Gets the timestamp when the user was last updated.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; init; }
}
