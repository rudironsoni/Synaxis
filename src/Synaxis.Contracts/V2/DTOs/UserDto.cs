using Synaxis.Contracts.V2.Common;

namespace Synaxis.Contracts.V2.DTOs;

/// <summary>
/// Data transfer object for a user (V2).
/// </summary>
/// <remarks>
/// V2 Breaking Changes:
/// - Added TenantId for multi-tenancy
/// - IsAdmin flag replaces roles list
/// - Added Metadata for extensibility
/// </remarks>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(UserDto), "user")]
public record UserDto
{
    /// <summary>
    /// Unique identifier of the user.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public required Guid Id { get; init; }

    /// <summary>
    /// Identifier of the tenant.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("tenantId")]
    public string? TenantId { get; init; }

    /// <summary>
    /// Email address of the user.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("email")]
    public required string Email { get; init; }

    /// <summary>
    /// Display name of the user.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("displayName")]
    public required string DisplayName { get; init; }

    /// <summary>
    /// Current status of the user.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public required UserStatus Status { get; init; }

    /// <summary>
    /// Whether the user has administrator privileges.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("isAdmin")]
    public bool IsAdmin { get; init; }

    /// <summary>
    /// Metadata for extensibility.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// Timestamp when the user was created.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Timestamp when the user was last updated.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }

    /// <summary>
    /// Timestamp when the user was last active.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("lastActiveAt")]
    public DateTimeOffset? LastActiveAt { get; init; }
}
