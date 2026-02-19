namespace Synaxis.Contracts.V2.Commands;

/// <summary>
/// Command to update an existing user (V2).
/// </summary>
/// <remarks>
/// V2 Breaking Changes:
/// - Uses UpdateMask for partial updates
/// - IsAdmin replaces roles list
/// </remarks>
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
    /// Fields to update.
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
    /// New password (null if not changing).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("password")]
    public string? Password { get; init; }

    /// <summary>
    /// Updated admin status.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("isAdmin")]
    public bool? IsAdmin { get; init; }

    /// <summary>
    /// Updated status.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public string? Status { get; init; }

    /// <summary>
    /// Updated metadata (null if not changing).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; init; }
}
