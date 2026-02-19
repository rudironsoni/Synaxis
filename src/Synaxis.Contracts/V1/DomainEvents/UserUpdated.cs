using Synaxis.Contracts.V1.Common;

namespace Synaxis.Contracts.V1.DomainEvents;

/// <summary>
/// Event raised when a user is updated.
/// </summary>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(UserUpdated), "user_updated")]
public record UserUpdated : DomainEventBase
{
    /// <summary>
    /// Updated email address (null if unchanged).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("email")]
    public string? Email { get; init; }

    /// <summary>
    /// Updated display name (null if unchanged).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }

    /// <summary>
    /// Updated status (null if unchanged).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public UserStatus? Status { get; init; }

    /// <summary>
    /// Updated roles (null if unchanged).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("roles")]
    public IReadOnlyList<string>? Roles { get; init; }

    /// <summary>
    /// Timestamp when the user was last updated.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; init; }
}
