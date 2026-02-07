// <copyright file="SecurityAlert.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.RealTime
{
    using System;

    /// <summary>
    /// Real-time security alert notification.
    /// </summary>
    /// <param name="organizationId">The organization identifier.</param>
    /// <param name="alertType">The type of alert (e.g., "weak_secret", "rate_limit_missing").</param>
    /// <param name="severity">The severity level (e.g., "critical", "warning", "info").</param>
    /// <param name="message">The alert message.</param>
    /// <param name="detectedAt">The date and time when the alert was detected.</param>
    public record SecurityAlert(
        Guid organizationId,
        string alertType,
        string severity,
        string message,
        DateTime detectedAt);
}
