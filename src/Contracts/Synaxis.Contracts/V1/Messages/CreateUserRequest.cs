// <copyright file="CreateUserRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.Messages;

/// <summary>
/// Request contract for creating a new user in version 1 of the contracts.
/// </summary>
/// <param name="UserId">Unique identifier for the user.</param>
/// <param name="TenantId">Identifier of the tenant the user belongs to.</param>
/// <param name="Email">Email address of the user.</param>
/// <param name="Username">Username for authentication.</param>
/// <param name="DisplayName">Display name of the user.</param>
/// <param name="Roles">List of roles assigned to the user.</param>
/// <param name="RequestedBy">Identifier of the user requesting the user creation.</param>
public record CreateUserRequest(
    Guid UserId,
    string TenantId,
    string Email,
    string Username,
    string DisplayName,
    IList<string> Roles,
    string RequestedBy);
