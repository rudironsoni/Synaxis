// <copyright file="UserPasswordChanged.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.Domain.Events;

using Mediator;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when a user's password is changed.
/// </summary>
public sealed record UserPasswordChanged : IDomainEvent, INotification
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
}
