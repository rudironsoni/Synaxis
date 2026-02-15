// <copyright file="MessageBusOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Extensions.DI;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Configuration settings for message bus implementation.
/// </summary>
public class MessageBusOptions
{
    /// <summary>
    /// Gets or sets the message bus provider type (e.g., "AzureServiceBus", "AWSSQS", "GCPPubSub", "RabbitMQ").
    /// </summary>
    [Required(ErrorMessage = "Message bus provider is required")]
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the connection string for the message bus.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default topic/queue name.
    /// </summary>
    public string DefaultTopic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subscription name for receiving messages.
    /// </summary>
    public string SubscriptionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum delivery count before dead-lettering.
    /// </summary>
    [Range(1, 100, ErrorMessage = "Max delivery count must be between 1 and 100")]
    public int MaxDeliveryCount { get; set; } = 10;

    /// <summary>
    /// Gets or sets the message time-to-live in seconds.
    /// </summary>
    [Range(1, 604800, ErrorMessage = "Message TTL must be between 1 and 604800 seconds")]
    public int MessageTtlSeconds { get; set; } = 86400;

    /// <summary>
    /// Gets or sets a value indicating whether to enable dead-letter queue.
    /// </summary>
    public bool EnableDeadLetterQueue { get; set; } = true;

    /// <summary>
    /// Gets or sets the prefetch count for message consumers.
    /// </summary>
    [Range(0, 1000, ErrorMessage = "Prefetch count must be between 0 and 1000")]
    public int PrefetchCount { get; set; } = 10;
}
