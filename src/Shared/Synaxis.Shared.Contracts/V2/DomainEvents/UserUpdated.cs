// <copyright file="UserUpdated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Contracts.V2.DomainEvents;

using Synaxis.Shared.Contracts.V2.Common;

/// <summary>
/// Event raised when a user is updated (V2).
/// </summary>
/// <remarks>
/// V2 Breaking Changes:
/// - Uses Patch semantics with UpdateMask for partial updates
/// - Added PreviousValues for audit trail.
/// </remarks>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(UserUpdated), "user_updated")]
public record UserUpdated : DomainEventBase
{
    /// <summary>
    /// Gets the fields that were updated.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("updateMask")]
    public required IReadOnlyList<string> UpdateMask { get; init; }

    /// <summary>
    /// Gets the updated email address.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("email")]
    public string? Email { get; init; }

    /// <summary>
    /// Gets the updated display name.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }

    /// <summary>
    /// Gets the updated status.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public UserStatus? Status { get; init; }

    /// <summary>
    /// Gets the updated admin status.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("isAdmin")]
    public bool? IsAdmin { get; init; }

    /// <summary>
    /// Gets the previous values for audit trail.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("previousValues")]
    public IReadOnlyDictionary<string, object>? PreviousValues { get; init; }

    /// <summary>
    /// Gets the timestamp when the user was last updated.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; init; }
}
