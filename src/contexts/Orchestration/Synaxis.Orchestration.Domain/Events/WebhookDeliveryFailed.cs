// <copyright file="WebhookDeliveryFailed.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Domain.Events;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when a webhook delivery fails.
/// </summary>
public class WebhookDeliveryFailed : DomainEvent
{
    /// <summary>
    /// Gets or sets the webhook identifier.
    /// </summary>
    public Guid WebhookId { get; set; }

    /// <summary>
    /// Gets or sets the failed event type.
    /// </summary>
    public string FailedEventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.WebhookId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(WebhookDeliveryFailed);
}
