// <copyright file="CreateUserCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V2.Commands;

/// <summary>
/// Command to create a new user (V2).
/// </summary>
/// <remarks>
/// V2 Breaking Changes:
/// - Added TenantId for multi-tenancy
/// - IsAdmin flag replaces roles list
/// - Added Metadata for extensibility.
/// </remarks>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(CreateUserCommand), "create_user")]
public record CreateUserCommand : CommandBase
{
    /// <summary>
    /// Gets the email address of the new user.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("email")]
    public required string Email { get; init; }

    /// <summary>
    /// Gets the display name for the user.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("displayName")]
    public required string DisplayName { get; init; }

    /// <summary>
    /// Gets the initial password for the user (will be hashed).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("password")]
    public required string Password { get; init; }

    /// <summary>
    /// Gets a value indicating whether the user should have administrator privileges.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("isAdmin")]
    public bool IsAdmin { get; init; }

    /// <summary>
    /// Gets the metadata for extensibility.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
