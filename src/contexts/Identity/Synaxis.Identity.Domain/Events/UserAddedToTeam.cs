// <copyright file="UserAddedToTeam.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.Domain.Events;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when a user is added to a team.
/// </summary>
/// <param name="EventId">The unique identifier for the event.</param>
/// <param name="OccurredOn">The timestamp when the event occurred.</param>
/// <param name="EventType">The type name of the event.</param>
/// <param name="TeamId">The unique identifier of the team.</param>
/// <param name="UserId">The unique identifier of the user.</param>
/// <param name="Role">The role assigned to the user in the team.</param>
public sealed record UserAddedToTeam(
    string EventId,
    DateTime OccurredOn,
    string EventType,
    string TeamId,
    string UserId,
    string Role) : IDomainEvent;
