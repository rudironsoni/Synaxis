namespace Synaxis.Contracts.V2.Queries;

/// <summary>
/// Query to get a user by their identifier (V2).
/// </summary>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(GetUserByIdQuery), "get_user_by_id")]
public record GetUserByIdQuery : QueryBase
{
    /// <summary>
    /// Identifier of the user to retrieve.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("targetUserId")]
    public required Guid TargetUserId { get; init; }

    /// <summary>
    /// Whether to include soft-deleted users.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("includeDeleted")]
    public bool IncludeDeleted { get; init; }

    /// <summary>
    /// Whether to include metadata.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("includeMetadata")]
    public bool IncludeMetadata { get; init; }
}
