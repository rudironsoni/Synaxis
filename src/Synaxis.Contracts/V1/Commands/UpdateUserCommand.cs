namespace Synaxis.Contracts.V1.Commands;

/// <summary>
/// Command to update an existing user.
/// </summary>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(UpdateUserCommand), "update_user")]
public record UpdateUserCommand : CommandBase
{
    /// <summary>
    /// Identifier of the user to update.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("targetUserId")]
    public required Guid TargetUserId { get; init; }

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
    /// New password (null if not changing).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("password")]
    public string? Password { get; init; }

    /// <summary>
    /// Updated roles (null if unchanged).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("roles")]
    public IReadOnlyList<string>? Roles { get; init; }

    /// <summary>
    /// Updated status (null if unchanged).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public string? Status { get; init; }
}
