// <copyright file="CreateTenantResponse.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.Messages;

/// <summary>
/// Response contract for tenant creation in version 1 of the contracts.
/// </summary>
/// <param name="TenantId">Unique identifier of the created tenant.</param>
/// <param name="Name">Display name of the tenant.</param>
/// <param name="CreatedAt">Timestamp when the tenant was created.</param>
/// <param name="Status">Status of the tenant creation operation.</param>
/// <param name="Message">Optional message providing additional details.</param>
public record CreateTenantResponse(
    Guid TenantId,
    string Name,
    DateTime CreatedAt,
    string Status,
    string? Message);
