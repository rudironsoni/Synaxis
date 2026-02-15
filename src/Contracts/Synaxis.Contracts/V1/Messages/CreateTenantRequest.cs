// <copyright file="CreateTenantRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.Messages;

/// <summary>
/// Request contract for creating a new tenant in version 1 of the contracts.
/// </summary>
/// <param name="TenantId">Unique identifier for the tenant.</param>
/// <param name="Name">Display name of the tenant.</param>
/// <param name="Description">Optional description of the tenant.</param>
/// <param name="AdminEmail">Email address of the tenant administrator.</param>
/// <param name="RequestedBy">Identifier of the user requesting the tenant creation.</param>
public record CreateTenantRequest(
    Guid TenantId,
    string Name,
    string? Description,
    string AdminEmail,
    string RequestedBy);
