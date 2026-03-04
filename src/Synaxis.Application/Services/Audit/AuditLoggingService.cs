// <copyright file="AuditLoggingService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Application.Services.Audit;

using Microsoft.Extensions.Logging;
using Synaxis.Core.Contracts;
using Synaxis.Core.Models;

/// <summary>
/// Application-level service for logging audit events.
/// Validates events and delegates to the infrastructure audit service.
/// </summary>
public class AuditLoggingService : IAuditLoggingService
{
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditLoggingService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditLoggingService"/> class.
    /// </summary>
    /// <param name="auditService">The audit service.</param>
    /// <param name="logger">The logger.</param>
    public AuditLoggingService(
        IAuditService auditService,
        ILogger<AuditLoggingService> logger)
    {
        this._auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task LogEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        if (!auditEvent.IsValid())
        {
            this._logger.LogWarning("Invalid audit event rejected: {EventType}", auditEvent.EventType);
            throw new ArgumentException("Invalid audit event", nameof(auditEvent));
        }

        try
        {
            await this._auditService.LogEventAsync(auditEvent).ConfigureAwait(false);
            this._logger.LogDebug("Audit event logged: {EventType} by {UserId}",
                auditEvent.EventType, auditEvent.UserId);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to log audit event: {EventType}", auditEvent.EventType);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task LogBatchAsync(IEnumerable<AuditEvent> auditEvents, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvents);

        var events = auditEvents.ToList();
        if (events.Count == 0)
        {
            return;
        }

        var invalidEvents = events.Where(e => !e.IsValid()).ToList();
        if (invalidEvents.Any())
        {
            this._logger.LogWarning("{Count} invalid audit events rejected", invalidEvents.Count);
            throw new ArgumentException("One or more audit events are invalid", nameof(auditEvents));
        }

        try
        {
            await this._auditService.LogEventBatchAsync(events, cancellationToken).ConfigureAwait(false);
            this._logger.LogDebug("Batch of {Count} audit events logged", events.Count);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to log batch of {Count} audit events", events.Count);
            throw;
        }
    }
}
