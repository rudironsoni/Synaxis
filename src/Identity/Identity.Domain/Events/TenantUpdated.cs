// <copyright file="TenantUpdated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.Domain.Events;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when a tenant is updated.
/// </summary>
/// <param name="EventId">The unique identifier for the event.</param>
/// <param name="OccurredOn">The timestamp when the event occurred.</param>
/// <param name="EventType">The type name of the event.</param>
/// <param name="TenantId">The unique identifier of the tenant.</param>
/// <param name="Name">The updated name of the tenant.</param>
/// <param name="PrimaryRegion">The updated primary region of the tenant.</param>
public sealed record TenantUpdated(
    string EventId,
    DateTime OccurredOn,
    string EventType,
    string TenantId,
    string Name,
    string PrimaryRegion) : IDomainEvent;
