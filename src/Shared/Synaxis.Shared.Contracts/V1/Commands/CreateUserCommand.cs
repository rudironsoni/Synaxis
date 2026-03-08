// <copyright file="CreateUserCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Contracts.V1.Commands;

/// <summary>
/// Command to create a new user.
/// </summary>
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
    /// Gets the roles to assign to the user.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("roles")]
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
}
