// <copyright file="WebhookUpdated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Domain.Events;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when a webhook is updated.
/// </summary>
public class WebhookUpdated : DomainEvent
{
    /// <summary>
    /// Gets or sets the webhook identifier.
    /// </summary>
    public Guid WebhookId { get; set; }

    /// <summary>
    /// Gets or sets the webhook name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTTP method.
    /// </summary>
    public string HttpMethod { get; set; } = "POST";

    /// <summary>
    /// Gets or sets the secret for HMAC signature.
    /// </summary>
    public string? Secret { get; set; }

    /// <summary>
    /// Gets or sets the event types to subscribe to.
    /// </summary>
    public IList<string> EventTypes { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.WebhookId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(WebhookUpdated);
}
