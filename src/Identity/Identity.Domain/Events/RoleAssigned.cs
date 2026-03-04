// <copyright file="RoleAssigned.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.Domain.Events;

using Mediator;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when a role is assigned to a user.
/// </summary>
public sealed record RoleAssigned : IDomainEvent, INotification
{
    /// <inheritdoc/>
    public required string EventId { get; init; }

    /// <inheritdoc/>
    public required DateTime OccurredOn { get; init; }

    /// <inheritdoc/>
    public required string EventType { get; init; }

    /// <summary>
    /// Gets the unique identifier of the user.
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// Gets the unique identifier of the role.
    /// </summary>
    public required string RoleId { get; init; }

    /// <summary>
    /// Gets the name of the role.
    /// </summary>
    public required string RoleName { get; init; }

    /// <summary>
    /// Gets the identifier of the user who assigned the role.
    /// </summary>
    public required string AssignedBy { get; init; }
}
