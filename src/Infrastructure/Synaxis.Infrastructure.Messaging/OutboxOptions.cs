// <copyright file="OutboxOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Messaging;

/// <summary>
/// Configuration options for the outbox processor.
/// </summary>
public class OutboxOptions
{
    /// <summary>
    /// Gets or sets the polling interval in seconds.
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the batch size for processing messages.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts.
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;
}
