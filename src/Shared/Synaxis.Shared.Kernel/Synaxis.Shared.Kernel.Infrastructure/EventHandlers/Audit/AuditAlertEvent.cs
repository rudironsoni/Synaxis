// <copyright file="AuditAlertEvent.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Kernel.Infrastructure.EventHandlers.Audit;

using System;
using System.Collections.Generic;
using MediatR;

/// <summary>
/// Event raised when a security alert is triggered from audit log analysis.
/// </summary>
public record AuditAlertEvent : IDomainEvent, INotification
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
    public string EventType => nameof(AuditAlertEvent);

    /// <summary>
    /// Gets the organization identifier.
    /// </summary>
    public required Guid OrganizationId { get; init; }

    /// <summary>
    /// Gets the user identifier (if applicable).
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// Gets the type of alert (e.g., "BruteForceLogin", "UnusualAccess", "DataExfiltration").
    /// </summary>
    public required string AlertType { get; init; }

    /// <summary>
    /// Gets the severity level (e.g., "Low", "Medium", "High", "Critical").
    /// </summary>
    public required string Severity { get; init; }

    /// <summary>
    /// Gets the alert message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets additional metadata about the alert.
    /// </summary>
    public IDictionary<string, object>? Metadata { get; init; }
}
