// <copyright file="UserUpdated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.Domain.Events;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when a user is updated.
/// </summary>
/// <param name="EventId">The unique identifier for the event.</param>
/// <param name="OccurredOn">The timestamp when the event occurred.</param>
/// <param name="EventType">The type name of the event.</param>
/// <param name="UserId">The unique identifier of the user.</param>
/// <param name="FirstName">The updated first name of the user.</param>
/// <param name="LastName">The updated last name of the user.</param>
public sealed record UserUpdated(
    string EventId,
    DateTime OccurredOn,
    string EventType,
    string UserId,
    string FirstName,
    string LastName) : IDomainEvent;
