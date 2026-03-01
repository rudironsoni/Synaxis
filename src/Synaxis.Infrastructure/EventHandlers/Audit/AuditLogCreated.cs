// <copyright file="AuditLogCreated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.EventHandlers.Audit;

using System;
using MediatR;
using Synaxis.Core.Models;

/// <summary>
/// Event raised when an audit log is created.
/// </summary>
public record AuditLogCreated : IDomainEvent, INotification
{
    /// <summary>
    /// Gets the unique identifier for this event.
    /// </summary>
    public string EventId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets when this event occurred.
    /// </summary>
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the type of this event.
    /// </summary>
    public string EventType => nameof(AuditLogCreated);

    /// <summary>
    /// Gets the audit log that was created.
    /// </summary>
    public required AuditLog AuditLog { get; init; }
}
