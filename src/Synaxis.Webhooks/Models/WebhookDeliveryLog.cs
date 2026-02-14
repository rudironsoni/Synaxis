// <copyright file="WebhookDeliveryLog.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Webhooks.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    /// <summary>
    /// Represents a log entry for a webhook delivery attempt.
    /// </summary>
    [Table("WebhookDeliveryLogs")]
    public class WebhookDeliveryLog
    {
        /// <summary>
        /// Gets or sets the unique identifier for the delivery log.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the webhook ID this delivery log belongs to.
        /// </summary>
        [Required]
        public Guid WebhookId { get; set; }

        /// <summary>
        /// Gets or sets the webhook this delivery log belongs to.
        /// </summary>
        [ForeignKey(nameof(WebhookId))]
        public Webhook Webhook { get; set; } = null!;

        /// <summary>
        /// Gets or sets the event type that was delivered.
        /// </summary>
        [Required]
        [MaxLength(256)]
        public string EventType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the payload that was sent.
        /// </summary>
        [Required]
        public string Payload { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the HTTP status code of the delivery response.
        /// </summary>
        public int? StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the response body from the webhook endpoint.
        /// </summary>
        public string ResponseBody { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the error message if the delivery failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the retry attempt number (0 for first attempt).
        /// </summary>
        public int RetryAttempt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the delivery was successful.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the delivery was attempted.
        /// </summary>
        public DateTime DeliveredAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the duration of the delivery attempt in milliseconds.
        /// </summary>
        public long DurationMs { get; set; }
    }
}
