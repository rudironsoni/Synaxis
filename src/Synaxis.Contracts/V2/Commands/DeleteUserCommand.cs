namespace Synaxis.Contracts.V2.Commands;

/// <summary>
/// Command to delete a user (V2).
/// </summary>
/// <remarks>
/// V2 Breaking Changes:
/// - Added Anonymize flag for GDPR compliance
/// - Added DataRetentionPeriod for soft deletes
/// </remarks>
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

    /// <summary>
    /// Whether to anonymize user data for GDPR compliance.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("anonymize")]
    public bool Anonymize { get; init; }

    /// <summary>
    /// Data retention period for soft-deleted users.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("dataRetentionPeriod")]
    public TimeSpan? DataRetentionPeriod { get; init; }
}
