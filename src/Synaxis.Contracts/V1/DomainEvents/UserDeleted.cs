// <copyright file="UserDeleted.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.DomainEvents;

/// <summary>
/// Event raised when a user is deleted.
/// </summary>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(UserDeleted), "user_deleted")]
public record UserDeleted : DomainEventBase
{
    /// <summary>
    /// Gets the email address of the deleted user.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("email")]
    public required string Email { get; init; }

    /// <summary>
    /// Gets the reason for deletion.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("reason")]
    public string? Reason { get; init; }

    /// <summary>
    /// Gets the timestamp when the user was deleted.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("deletedAt")]
    public DateTimeOffset DeletedAt { get; init; }

    /// <summary>
    /// Gets a value indicating whether the deletion was permanent or soft delete.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("permanent")]
    public bool Permanent { get; init; }
}
