// <copyright file="WebhookDto.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Webhooks.Controllers;

using System.Collections.Generic;

/// <summary>
/// DTO for a webhook.
/// </summary>
public class WebhookDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the webhook.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the URL where webhook events will be sent.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the secret key used for HMAC-SHA256 signature verification.
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of events this webhook subscribes to.
    /// </summary>
    public IList<string> Events { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets a value indicating whether the webhook is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the organization ID that owns this webhook.
    /// </summary>
    public Guid OrganizationId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the webhook was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the webhook was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the webhook was last successfully delivered.
    /// </summary>
    public DateTime LastSuccessfulDeliveryAt { get; set; }

    /// <summary>
    /// Gets or sets the number of consecutive failed delivery attempts.
    /// </summary>
    public int FailedDeliveryAttempts { get; set; }
}
