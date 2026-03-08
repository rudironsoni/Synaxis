// <copyright file="UserLoggedIn.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.Domain.Events;

using Mediator;
using Synaxis.Shared.Kernel.Application.Cloud;

/// <summary>
/// Event raised when a user successfully logs in.
/// </summary>
public sealed record UserLoggedIn : IDomainEvent, INotification
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
    /// Gets the IP address of the login attempt.
    /// </summary>
    public required string IpAddress { get; init; }

    /// <summary>
    /// Gets the user agent of the login attempt.
    /// </summary>
    public required string UserAgent { get; init; }
}
