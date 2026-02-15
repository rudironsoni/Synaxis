// <copyright file="EventStoreOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Extensions.DI;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Configuration settings for event store implementation.
/// </summary>
public class EventStoreOptions
{
    /// <summary>
    /// Gets or sets the event store provider type (e.g., "InMemory", "CosmosDB", "EventStoreDB").
    /// </summary>
    [Required(ErrorMessage = "Event store provider is required")]
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the connection string for the event store.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the database name for event storage.
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the container/collection name for events.
    /// </summary>
    public string ContainerName { get; set; } = "events";

    /// <summary>
    /// Gets or sets the snapshot strategy type (e.g., "Interval", "Version", "None").
    /// </summary>
    public string SnapshotStrategy { get; set; } = "None";

    /// <summary>
    /// Gets or sets the snapshot interval (number of events between snapshots).
    /// </summary>
    [Range(1, 10000, ErrorMessage = "Snapshot interval must be between 1 and 10000")]
    public int SnapshotInterval { get; set; } = 100;

    /// <summary>
    /// Gets or sets a value indicating whether to enable event compression.
    /// </summary>
    public bool EnableCompression { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum batch size for event operations.
    /// </summary>
    [Range(1, 1000, ErrorMessage = "Max batch size must be between 1 and 1000")]
    public int MaxBatchSize { get; set; } = 100;
}
