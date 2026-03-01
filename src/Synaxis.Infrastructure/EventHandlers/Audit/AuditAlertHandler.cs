// <copyright file="AuditAlertHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.EventHandlers.Audit;

using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;

/// <summary>
/// Handles audit alert events by logging and sending notifications.
/// </summary>
public class AuditAlertHandler : INotificationHandler<AuditAlertEvent>
{
    private readonly ILogger<AuditAlertHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditAlertHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public AuditAlertHandler(ILogger<AuditAlertHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        this._logger = logger;
    }

    /// <inheritdoc/>
    public Task Handle(AuditAlertEvent notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        // Log the alert based on severity
        this.LogAlert(notification);

        // In a production system, you would:
        // 1. Send email notifications to security team
        // 2. Send webhook notifications to SIEM systems
        // 3. Store alerts in a dedicated alerts table
        // 4. Trigger automated incident response

        return Task.CompletedTask;
    }

    /// <summary>
    /// Logs the alert based on its severity level.
    /// </summary>
    private void LogAlert(AuditAlertEvent alert)
    {
        var message = $"Security Alert [{alert.AlertType}]: {alert.Message} " +
                      $"(Organization: {alert.OrganizationId}, User: {alert.UserId}, Severity: {alert.Severity})";

        switch (alert.Severity.ToLowerInvariant())
        {
            case "critical":
                this._logger.LogCritical("{Message}", message);
                break;
            case "high":
                this._logger.LogError("{Message}", message);
                break;
            case "medium":
                this._logger.LogWarning("{Message}", message);
                break;
            case "low":
                this._logger.LogInformation("{Message}", message);
                break;
            default:
                this._logger.LogInformation("{Message}", message);
                break;
        }
    }
}
