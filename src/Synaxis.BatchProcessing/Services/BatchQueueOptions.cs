// <copyright file="BatchQueueOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.BatchProcessing.Services;

/// <summary>
/// Configuration options for batch queue.
/// </summary>
public class BatchQueueOptions
{
    /// <summary>
    /// Gets or sets the Azure Service Bus connection string.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the queue name.
    /// </summary>
    public string QueueName { get; set; } = "batch-processing";

    /// <summary>
    /// Gets or sets the maximum concurrent calls.
    /// </summary>
    public int MaxConcurrentCalls { get; set; } = 5;
}
