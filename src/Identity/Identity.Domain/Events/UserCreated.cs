// <copyright file="UserCreated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.Domain.Events;

using Mediator;
using Synaxis.Shared.Kernel.Application.Cloud;

/// <summary>
/// Event raised when a new user is created.
/// </summary>
public sealed record UserCreated : IDomainEvent, INotification
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
    /// Gets the email address of the user.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Gets the first name of the user.
    /// </summary>
    public required string FirstName { get; init; }

    /// <summary>
    /// Gets the last name of the user.
    /// </summary>
    public required string LastName { get; init; }

    /// <summary>
    /// Gets the unique identifier of the tenant.
    /// </summary>
    public required string TenantId { get; init; }
}
