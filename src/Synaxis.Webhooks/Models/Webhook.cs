// <copyright file="Webhook.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Webhooks.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    /// <summary>
    /// Represents a webhook configuration for event notifications.
    /// </summary>
    [Table("Webhooks")]
    public class Webhook
    {
        /// <summary>
        /// Gets or sets the unique identifier for the webhook.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the URL where webhook events will be sent.
        /// </summary>
        [Required]
        [Url]
        [MaxLength(2048)]
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the secret key used for HMAC-SHA256 signature verification.
        /// </summary>
        [Required]
        [MaxLength(256)]
        public string Secret { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of events this webhook subscribes to.
        /// </summary>
        [Required]
        public List<string> Events { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets a value indicating whether the webhook is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the organization ID that owns this webhook.
        /// </summary>
        [Required]
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the webhook was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the timestamp when the webhook was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the timestamp when the webhook was last successfully delivered.
        /// </summary>
        public DateTime LastSuccessfulDeliveryAt { get; set; }

        /// <summary>
        /// Gets or sets the number of consecutive failed delivery attempts.
        /// </summary>
        public int FailedDeliveryAttempts { get; set; }

        /// <summary>
        /// Gets or sets the delivery logs for this webhook.
        /// </summary>
        public List<WebhookDeliveryLog> DeliveryLogs { get; set; } = new List<WebhookDeliveryLog>();
    }
}
