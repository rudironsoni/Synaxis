// <copyright file="UserDeleted.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V2.DomainEvents;

/// <summary>
/// Event raised when a user is deleted (V2).
/// </summary>
/// <remarks>
/// V2 Breaking Changes:
/// - Email is now optional (may not be available for GDPR deletion)
/// - Added Anonymized flag for GDPR compliance
/// - Added DataRetentionPeriod for compliance.
/// </remarks>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(UserDeleted), "user_deleted")]
public record UserDeleted : DomainEventBase
{
    /// <summary>
    /// Gets the email address of the deleted user (null if anonymized).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("email")]
    public string? Email { get; init; }

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

    /// <summary>
    /// Gets a value indicating whether user data was anonymized for GDPR compliance.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("anonymized")]
    public bool Anonymized { get; init; }

    /// <summary>
    /// Gets the data retention period for soft-deleted users.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("dataRetentionPeriod")]
    public TimeSpan? DataRetentionPeriod { get; init; }
}
