// <copyright file="TeamCreated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.Domain.Events;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when a new team is created.
/// </summary>
/// <param name="EventId">The unique identifier for the event.</param>
/// <param name="OccurredOn">The timestamp when the event occurred.</param>
/// <param name="EventType">The type name of the event.</param>
/// <param name="TeamId">The unique identifier of the team.</param>
/// <param name="Name">The name of the team.</param>
/// <param name="Description">The description of the team.</param>
/// <param name="TenantId">The unique identifier of the tenant.</param>
public sealed record TeamCreated(
    string EventId,
    DateTime OccurredOn,
    string EventType,
    string TeamId,
    string Name,
    string Description,
    string TenantId) : IDomainEvent;
