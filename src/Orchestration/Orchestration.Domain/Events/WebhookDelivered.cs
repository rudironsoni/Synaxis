// <copyright file="WebhookDelivered.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Domain.Events;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when a webhook is delivered successfully.
/// </summary>
public class WebhookDelivered : DomainEvent
{
    /// <summary>
    /// Gets or sets the webhook identifier.
    /// </summary>
    public Guid WebhookId { get; set; }

    /// <summary>
    /// Gets or sets the delivered event type.
    /// </summary>
    public string DeliveredEventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the payload.
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.WebhookId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(WebhookDelivered);
}
