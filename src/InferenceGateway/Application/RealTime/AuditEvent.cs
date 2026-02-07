// <copyright file="AuditEvent.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.RealTime
{
    using System;

    /// <summary>
    /// Real-time audit event notification.
    /// </summary>
    /// <param name="id">The event identifier.</param>
    /// <param name="action">The action that was performed.</param>
    /// <param name="entityType">The type of entity affected.</param>
    /// <param name="performedBy">The user who performed the action.</param>
    /// <param name="performedAt">The date and time when the action was performed.</param>
    public record AuditEvent(
        Guid id,
        string action,
        string entityType,
        string performedBy,
        DateTime performedAt);
}
