// <copyright file="UserCreated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Contracts.V2.DomainEvents;

using Synaxis.Shared.Contracts.V2.Common;

/// <summary>
/// Event raised when a new user is created (V2).
/// </summary>
/// <remarks>
/// V2 Breaking Changes:
/// - Added TenantId for multi-tenancy
/// - Email is now an EmailAddress record type
/// - Added Metadata dictionary for extensibility
/// - Removed Roles from creation (now assigned via separate event).
/// </remarks>
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
    /// Gets a value indicating whether the user is an administrator.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("isAdmin")]
    public bool IsAdmin { get; init; }

    /// <summary>
    /// Gets the metadata for extensibility.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// Gets the timestamp when the user was created.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }
}
