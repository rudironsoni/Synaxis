// <copyright file="UserLocked.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.Domain.Events;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when a user is locked.
/// </summary>
/// <param name="EventId">The unique identifier for the event.</param>
/// <param name="OccurredOn">The timestamp when the event occurred.</param>
/// <param name="EventType">The type name of the event.</param>
/// <param name="UserId">The unique identifier of the user.</param>
/// <param name="LockedUntil">The timestamp until which the user is locked.</param>
public sealed record UserLocked(
    string EventId,
    DateTime OccurredOn,
    string EventType,
    string UserId,
    DateTime LockedUntil) : IDomainEvent;
