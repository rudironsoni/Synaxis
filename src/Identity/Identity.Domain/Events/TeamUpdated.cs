// <copyright file="TeamUpdated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.Domain.Events;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when a team is updated.
/// </summary>
/// <param name="EventId">The unique identifier for the event.</param>
/// <param name="OccurredOn">The timestamp when the event occurred.</param>
/// <param name="EventType">The type name of the event.</param>
/// <param name="TeamId">The unique identifier of the team.</param>
/// <param name="Name">The updated name of the team.</param>
/// <param name="Description">The updated description of the team.</param>
public sealed record TeamUpdated(
    string EventId,
    DateTime OccurredOn,
    string EventType,
    string TeamId,
    string Name,
    string Description) : IDomainEvent;
