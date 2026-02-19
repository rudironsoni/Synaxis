using Synaxis.Contracts.V2.Common;

namespace Synaxis.Contracts.V2.DomainEvents;

/// <summary>
/// Event raised when a user is updated (V2).
/// </summary>
/// <remarks>
/// V2 Breaking Changes:
/// - Uses Patch semantics with UpdateMask for partial updates
/// - Added PreviousValues for audit trail
/// </remarks>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(UserUpdated), "user_updated")]
public record UserUpdated : DomainEventBase
{
    /// <summary>
    /// Fields that were updated.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("updateMask")]
    public required IReadOnlyList<string> UpdateMask { get; init; }

    /// <summary>
    /// Updated email address.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("email")]
    public string? Email { get; init; }

    /// <summary>
    /// Updated display name.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }

    /// <summary>
    /// Updated status.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public UserStatus? Status { get; init; }

    /// <summary>
    /// Updated admin status.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("isAdmin")]
    public bool? IsAdmin { get; init; }

    /// <summary>
    /// Previous values for audit trail.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("previousValues")]
    public Dictionary<string, object>? PreviousValues { get; init; }

    /// <summary>
    /// Timestamp when the user was last updated.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; init; }
}
