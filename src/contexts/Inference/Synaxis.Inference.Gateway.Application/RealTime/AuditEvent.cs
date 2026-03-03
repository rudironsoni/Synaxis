// <copyright file="AuditEvent.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.RealTime
{
    using System;

    /// <summary>
    /// Real-time audit event notification.
    /// </summary>
    /// <param name="Id">The event identifier.</param>
    /// <param name="Action">The action that was performed.</param>
    /// <param name="EntityType">The type of entity affected.</param>
    /// <param name="PerformedBy">The user who performed the action.</param>
    /// <param name="PerformedAt">The date and time when the action was performed.</param>
    public record AuditEvent(
        Guid Id,
        string Action,
        string EntityType,
        string PerformedBy,
        DateTime PerformedAt);
}
