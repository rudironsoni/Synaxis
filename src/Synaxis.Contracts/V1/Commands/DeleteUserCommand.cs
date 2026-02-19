namespace Synaxis.Contracts.V1.Commands;

/// <summary>
/// Command to delete a user.
/// </summary>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(DeleteUserCommand), "delete_user")]
public record DeleteUserCommand : CommandBase
{
    /// <summary>
    /// Identifier of the user to delete.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("targetUserId")]
    public required Guid TargetUserId { get; init; }

    /// <summary>
    /// Reason for deletion.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("reason")]
    public string? Reason { get; init; }

    /// <summary>
    /// Whether to permanently delete (true) or soft delete (false).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("permanent")]
    public bool Permanent { get; init; }
}
