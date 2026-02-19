namespace Synaxis.Contracts.V1.DomainEvents;

/// <summary>
/// Event raised when a user is deleted.
/// </summary>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(UserDeleted), "user_deleted")]
public record UserDeleted : DomainEventBase
{
    /// <summary>
    /// Email address of the deleted user.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("email")]
    public required string Email { get; init; }

    /// <summary>
    /// Reason for deletion.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("reason")]
    public string? Reason { get; init; }

    /// <summary>
    /// Timestamp when the user was deleted.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("deletedAt")]
    public DateTimeOffset DeletedAt { get; init; }

    /// <summary>
    /// Whether the deletion was permanent or soft delete.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("permanent")]
    public bool Permanent { get; init; }
}
