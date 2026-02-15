// <copyright file="CreateUserResponse.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.Messages;

/// <summary>
/// Response contract for user creation in version 1 of the contracts.
/// </summary>
/// <param name="UserId">Unique identifier of the created user.</param>
/// <param name="TenantId">Identifier of the tenant the user belongs to.</param>
/// <param name="Email">Email address of the user.</param>
/// <param name="Username">Username of the user.</param>
/// <param name="DisplayName">Display name of the user.</param>
/// <param name="CreatedAt">Timestamp when the user was created.</param>
/// <param name="Status">Status of the user creation operation.</param>
/// <param name="Message">Optional message providing additional details.</param>
public record CreateUserResponse(
    Guid UserId,
    string TenantId,
    string Email,
    string Username,
    string DisplayName,
    DateTime CreatedAt,
    string Status,
    string? Message);
