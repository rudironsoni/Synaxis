// <copyright file="PermissionRevoked.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.Domain.Events;

using Mediator;
using Synaxis.Shared.Kernel.Application.Cloud;

/// <summary>
/// Event raised when a permission is revoked from a user or role.
/// </summary>
public sealed record PermissionRevoked : IDomainEvent, INotification
{
    /// <inheritdoc/>
    public required string EventId { get; init; }

    /// <inheritdoc/>
    public required DateTime OccurredOn { get; init; }

    /// <inheritdoc/>
    public required string EventType { get; init; }

    /// <summary>
    /// Gets the unique identifier of the user, if applicable.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Gets the unique identifier of the role, if applicable.
    /// </summary>
    public string? RoleId { get; init; }

    /// <summary>
    /// Gets the permission being revoked.
    /// </summary>
    public required string Permission { get; init; }

    /// <summary>
    /// Gets the type of resource the permission applies to.
    /// </summary>
    public required string ResourceType { get; init; }

    /// <summary>
    /// Gets the identifier of the resource, if applicable.
    /// </summary>
    public string? ResourceId { get; init; }

    /// <summary>
    /// Gets the identifier of the user who revoked the permission.
    /// </summary>
    public required string RevokedBy { get; init; }
}
