// <copyright file="WebhookDeleted.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Domain.Events;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when a webhook is deleted.
/// </summary>
public class WebhookDeleted : DomainEvent
{
    /// <summary>
    /// Gets or sets the webhook identifier.
    /// </summary>
    public Guid WebhookId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.WebhookId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(WebhookDeleted);
}
