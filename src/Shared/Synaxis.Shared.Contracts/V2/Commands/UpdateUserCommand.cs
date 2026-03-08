// <copyright file="UpdateUserCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Contracts.V2.Commands;

/// <summary>
/// Command to update an existing user (V2).
/// </summary>
/// <remarks>
/// V2 Breaking Changes:
/// - Uses UpdateMask for partial updates
/// - IsAdmin replaces roles list.
/// </remarks>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(UpdateUserCommand), "update_user")]
public record UpdateUserCommand : CommandBase
{
    /// <summary>
    /// Gets the identifier of the user to update.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("targetUserId")]
    public required Guid TargetUserId { get; init; }

    /// <summary>
    /// Gets the fields to update.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("updateMask")]
    public required IReadOnlyList<string> UpdateMask { get; init; }

    /// <summary>
    /// Gets the updated email address.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("email")]
    public string? Email { get; init; }

    /// <summary>
    /// Gets the updated display name.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }

    /// <summary>
    /// Gets the new password (null if not changing).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("password")]
    public string? Password { get; init; }

    /// <summary>
    /// Gets the updated admin status.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("isAdmin")]
    public bool? IsAdmin { get; init; }

    /// <summary>
    /// Gets the updated status.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public string? Status { get; init; }

    /// <summary>
    /// Gets the updated metadata (null if not changing).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
