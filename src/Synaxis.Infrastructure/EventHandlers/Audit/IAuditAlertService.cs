// <copyright file="IAuditAlertService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.EventHandlers.Audit;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Synaxis.Core.Models;

/// <summary>
/// Service for evaluating audit logs and generating security alerts.
/// </summary>
public interface IAuditAlertService
{
    /// <summary>
    /// Evaluates an audit log for suspicious patterns and generates alerts if needed.
    /// </summary>
    /// <param name="auditLog">The audit log to evaluate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task EvaluateForAlertsAsync(AuditLog auditLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active alerts for an organization.
    /// </summary>
    /// <param name="organizationId">The organization identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of active alerts.</returns>
    Task<IEnumerable<AuditAlertEvent>> GetActiveAlertsAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
