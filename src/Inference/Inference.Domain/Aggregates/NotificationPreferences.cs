// <copyright file="NotificationPreferences.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Aggregates;

/// <summary>
/// Represents notification preferences.
/// </summary>
public class NotificationPreferences
{
    /// <summary>
    /// Gets or sets a value indicating whether to enable email notifications.
    /// </summary>
    public bool EnableEmailNotifications { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to notify on quota threshold.
    /// </summary>
    public bool NotifyOnQuotaThreshold { get; set; } = true;

    /// <summary>
    /// Gets or sets the quota threshold percentage.
    /// </summary>
    public int QuotaThresholdPercent { get; set; } = 80;

    /// <summary>
    /// Gets or sets a value indicating whether to notify on long-running requests.
    /// </summary>
    public bool NotifyOnLongRunningRequests { get; set; } = false;

    /// <summary>
    /// Gets or sets the long-running threshold in seconds.
    /// </summary>
    public int LongRunningThresholdSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets a value indicating whether to notify on errors.
    /// </summary>
    public bool NotifyOnErrors { get; set; } = true;
}
