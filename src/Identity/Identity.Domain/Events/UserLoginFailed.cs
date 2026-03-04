// <copyright file="UserLoginFailed.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.Domain.Events;

using Mediator;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when a user login attempt fails.
/// </summary>
public sealed record UserLoginFailed : IDomainEvent, INotification
{
    /// <inheritdoc/>
    public required string EventId { get; init; }

    /// <inheritdoc/>
    public required DateTime OccurredOn { get; init; }

    /// <inheritdoc/>
    public required string EventType { get; init; }

    /// <summary>
    /// Gets the unique identifier of the user, if known.
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Gets the email address used for the login attempt.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Gets the reason for the login failure.
    /// </summary>
    public required string FailureReason { get; init; }

    /// <summary>
    /// Gets the IP address of the login attempt.
    /// </summary>
    public required string IpAddress { get; init; }

    /// <summary>
    /// Gets the user agent of the login attempt.
    /// </summary>
    public required string UserAgent { get; init; }
}
