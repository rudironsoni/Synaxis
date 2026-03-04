// <copyright file="PasswordResetRequested.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.Domain.Events;

using Mediator;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when a password reset is requested.
/// </summary>
public sealed record PasswordResetRequested : IDomainEvent, INotification
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
    /// Gets the password reset token.
    /// </summary>
    public required string ResetToken { get; init; }
}
