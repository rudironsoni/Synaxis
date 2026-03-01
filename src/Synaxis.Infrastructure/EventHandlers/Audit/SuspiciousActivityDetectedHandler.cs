// <copyright file="SuspiciousActivityDetectedHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.EventHandlers.Audit;

using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;

/// <summary>
/// Handles audit log created events and evaluates them for suspicious activity.
/// </summary>
public class SuspiciousActivityDetectedHandler : INotificationHandler<AuditLogCreated>
{
    private readonly IAuditAlertService _alertService;
    private readonly ILogger<SuspiciousActivityDetectedHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SuspiciousActivityDetectedHandler"/> class.
    /// </summary>
    /// <param name="alertService">The audit alert service.</param>
    /// <param name="logger">The logger.</param>
    public SuspiciousActivityDetectedHandler(
        IAuditAlertService alertService,
        ILogger<SuspiciousActivityDetectedHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(alertService);
        ArgumentNullException.ThrowIfNull(logger);
        _alertService = alertService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task Handle(AuditLogCreated notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        _logger.LogDebug(
            "Evaluating audit log {LogId} for suspicious activity",
            notification.AuditLog.Id);

        await _alertService.EvaluateForAlertsAsync(notification.AuditLog, cancellationToken).ConfigureAwait(false);
    }
}
