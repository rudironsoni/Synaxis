// <copyright file="AuditEventValidator.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Application.Services.Audit;

using Synaxis.Core.Contracts;

/// <summary>
/// Extension methods for validating audit events.
/// </summary>
public static class AuditEventValidator
{
    /// <summary>
    /// Validates an audit event.
    /// </summary>
    /// <param name="auditEvent">The audit event to validate.</param>
    /// <returns>True if the event is valid; otherwise, false.</returns>
    public static bool IsValid(this AuditEvent auditEvent)
    {
        if (auditEvent == null) return false;
        if (string.IsNullOrWhiteSpace(auditEvent.EventType)) return false;
        if (auditEvent.OrganizationId == Guid.Empty) return false;
        return true;
    }
}