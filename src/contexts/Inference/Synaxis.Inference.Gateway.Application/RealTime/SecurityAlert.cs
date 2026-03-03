// <copyright file="SecurityAlert.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.RealTime
{
    using System;

    /// <summary>
    /// Real-time security alert notification.
    /// </summary>
    /// <param name="OrganizationId">The organization identifier.</param>
    /// <param name="AlertType">The type of alert (e.g., "weak_secret", "rate_limit_missing").</param>
    /// <param name="Severity">The severity level (e.g., "critical", "warning", "info").</param>
    /// <param name="Message">The alert message.</param>
    /// <param name="DetectedAt">The date and time when the alert was detected.</param>
    public record SecurityAlert(
        Guid OrganizationId,
        string AlertType,
        string Severity,
        string Message,
        DateTime DetectedAt);
}
