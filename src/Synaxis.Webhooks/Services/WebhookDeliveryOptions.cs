// <copyright file="WebhookDeliveryOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Webhooks.Services;

/// <summary>
/// Configuration options for webhook delivery.
/// </summary>
public class WebhookDeliveryOptions
{
    /// <summary>
    /// Gets or sets the maximum number of retry attempts.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the initial delay between retries in seconds.
    /// </summary>
    public int InitialRetryDelaySeconds { get; set; } = 1;

    /// <summary>
    /// Gets or sets the maximum delay between retries in seconds.
    /// </summary>
    public int MaxRetryDelaySeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the timeout for webhook delivery in seconds.
    /// </summary>
    public int DeliveryTimeoutSeconds { get; set; } = 30;
}
