using Synaxis.Contracts.V1.Common;

namespace Synaxis.Contracts.V1.DomainEvents;

/// <summary>
/// Event raised when a new user is created.
/// </summary>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(UserCreated), "user_created")]
public record UserCreated : DomainEventBase
{
    /// <summary>
    /// Email address of the created user.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("email")]
    public required string Email { get; init; }

    /// <summary>
    /// Display name of the user.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("displayName")]
    public required string DisplayName { get; init; }

    /// <summary>
    /// Initial status of the user.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public required UserStatus Status { get; init; }

    /// <summary>
    /// Roles assigned to the user.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("roles")]
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Timestamp when the user was created.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }
}
