namespace Synaxis.Contracts.V1.Commands;

/// <summary>
/// Command to create a new user.
/// </summary>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(CreateUserCommand), "create_user")]
public record CreateUserCommand : CommandBase
{
    /// <summary>
    /// Email address of the new user.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("email")]
    public required string Email { get; init; }

    /// <summary>
    /// Display name for the user.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("displayName")]
    public required string DisplayName { get; init; }

    /// <summary>
    /// Initial password for the user (will be hashed).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("password")]
    public required string Password { get; init; }

    /// <summary>
    /// Roles to assign to the user.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("roles")]
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
}
