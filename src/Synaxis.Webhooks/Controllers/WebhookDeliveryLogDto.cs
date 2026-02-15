// <copyright file="WebhookDeliveryLogDto.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

#nullable enable

namespace Synaxis.Webhooks.Controllers;

/// <summary>
/// DTO for a webhook delivery log.
/// </summary>
public class WebhookDeliveryLogDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the delivery log.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the webhook ID this delivery log belongs to.
    /// </summary>
    public Guid WebhookId { get; set; }

    /// <summary>
    /// Gets or sets the event type that was delivered.
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the payload that was delivered.
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTTP status code of the delivery attempt.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the response body from the delivery attempt.
    /// </summary>
    public string ResponseBody { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the delivery was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the delivery was delivered.
    /// </summary>
    public DateTime DeliveredAt { get; set; }

    /// <summary>
    /// Gets or sets whether the delivery was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if the delivery failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the delivery was attempted.
    /// </summary>
    public DateTime AttemptedAt { get; set; }

    /// <summary>
    /// Gets or sets the duration of the delivery attempt in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the retry attempt number.
    /// </summary>
    public int RetryAttempt { get; set; }
}
