// <copyright file="IAuditLoggingService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Application.Services.Audit;

using Synaxis.Core.Contracts;

/// <summary>
/// Provides methods for logging audit events at the application layer.
/// </summary>
public interface IAuditLoggingService
{
    /// <summary>
    /// Logs a single audit event asynchronously.
    /// </summary>
    /// <param name="auditEvent">The audit event to log.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LogEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs a batch of audit events asynchronously.
    /// </summary>
    /// <param name="auditEvents">The audit events to log.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LogBatchAsync(IEnumerable<AuditEvent> auditEvents, CancellationToken cancellationToken = default);
}