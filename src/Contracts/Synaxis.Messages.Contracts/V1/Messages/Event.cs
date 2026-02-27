// <copyright file="Event.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.Messages;

/// <summary>
/// Base class for all domain events in version 1 of the contracts.
/// </summary>
/// <param name="EventId">Unique identifier for the event.</param>
/// <param name="OccurredAt">Timestamp when the event occurred.</param>
/// <param name="TenantId">Identifier of the tenant associated with the event.</param>
public abstract record Event(
    Guid EventId,
    DateTime OccurredAt,
    string TenantId);
