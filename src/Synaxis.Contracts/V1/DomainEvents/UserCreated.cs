// <copyright file="UserCreated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.DomainEvents;

using Synaxis.Contracts.V1.Common;

/// <summary>
/// Event raised when a new user is created.
/// </summary>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(UserCreated), "user_created")]
public record UserCreated : DomainEventBase
{
    /// <summary>
    /// Gets the email address of the created user.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("email")]
    public required string Email { get; init; }

    /// <summary>
    /// Gets the display name of the user.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("displayName")]
    public required string DisplayName { get; init; }

    /// <summary>
    /// Gets the initial status of the user.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public required UserStatus Status { get; init; }

    /// <summary>
    /// Gets the roles assigned to the user.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("roles")]
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets the timestamp when the user was created.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }
}
