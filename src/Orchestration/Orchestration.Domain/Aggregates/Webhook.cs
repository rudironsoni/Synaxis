// <copyright file="Webhook.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Domain.Aggregates;

using Synaxis.Infrastructure.EventSourcing;
using Synaxis.Orchestration.Domain.Events;

/// <summary>
/// Aggregate root representing a webhook for event-driven integrations.
/// </summary>
public class Webhook : AggregateRoot
{
    /// <summary>
    /// Gets the webhook identifier.
    /// </summary>
    public new Guid Id { get; private set; }

    /// <summary>
    /// Gets the webhook name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the target URL.
    /// </summary>
    public string Url { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the HTTP method.
    /// </summary>
    public string HttpMethod { get; private set; } = "POST";

    /// <summary>
    /// Gets the secret for HMAC signature.
    /// </summary>
    public string? Secret { get; private set; }

    /// <summary>
    /// Gets the event types to subscribe to.
    /// </summary>
    public List<string> EventTypes { get; private set; } = new();

    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Gets the current status.
    /// </summary>
    public WebhookStatus Status { get; private set; }

    /// <summary>
    /// Gets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the last delivery timestamp.
    /// </summary>
    public DateTime? LastDeliveredAt { get; private set; }

    /// <summary>
    /// Gets the delivery count.
    /// </summary>
    public int DeliveryCount { get; private set; }

    /// <summary>
    /// Gets the failure count.
    /// </summary>
    public int FailureCount { get; private set; }

    /// <summary>
    /// Creates a new webhook.
    /// </summary>
    public static Webhook Create(
        Guid id,
        string name,
        string url,
        string httpMethod,
        string? secret,
        List<string> eventTypes,
        Guid tenantId)
    {
        var webhook = new Webhook();
        var @event = new WebhookCreated
        {
            WebhookId = id,
            Name = name,
            Url = url,
            HttpMethod = httpMethod,
            Secret = secret,
            EventTypes = eventTypes,
            TenantId = tenantId,
            Timestamp = DateTime.UtcNow,
        };

        webhook.ApplyEvent(@event);
        return webhook;
    }

    /// <summary>
    /// Updates the webhook configuration.
    /// </summary>
    public void Update(string name, string url, string httpMethod, string? secret, List<string> eventTypes)
    {
        var @event = new WebhookUpdated
        {
            WebhookId = this.Id,
            Name = name,
            Url = url,
            HttpMethod = httpMethod,
            Secret = secret,
            EventTypes = eventTypes,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Activates the webhook.
    /// </summary>
    public void Activate()
    {
        if (this.Status == WebhookStatus.Active)
        {
            return;
        }

        var @event = new WebhookActivated
        {
            WebhookId = this.Id,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Deactivates the webhook.
    /// </summary>
    public void Deactivate()
    {
        if (this.Status == WebhookStatus.Inactive)
        {
            return;
        }

        var @event = new WebhookDeactivated
        {
            WebhookId = this.Id,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Records a successful delivery.
    /// </summary>
    public void RecordDelivery(string eventType, string payload)
    {
        var @event = new WebhookDelivered
        {
            WebhookId = this.Id,
            DeliveredEventType = eventType,
            Payload = payload,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Records a failed delivery.
    /// </summary>
    public void RecordFailure(string eventType, string errorMessage)
    {
        var @event = new WebhookDeliveryFailed
        {
            WebhookId = this.Id,
            FailedEventType = eventType,
            ErrorMessage = errorMessage,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Deletes the webhook.
    /// </summary>
    public void Delete()
    {
        var @event = new WebhookDeleted
        {
            WebhookId = this.Id,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <inheritdoc/>
    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case WebhookCreated created:
                this.ApplyCreated(created);
                break;
            case WebhookUpdated updated:
                this.ApplyUpdated(updated);
                break;
            case WebhookActivated:
                this.ApplyActivated();
                break;
            case WebhookDeactivated:
                this.ApplyDeactivated();
                break;
            case WebhookDelivered:
                this.ApplyDelivered();
                break;
            case WebhookDeliveryFailed:
                this.ApplyDeliveryFailed();
                break;
            case WebhookDeleted:
                this.ApplyDeleted();
                break;
        }
    }

    private void ApplyCreated(WebhookCreated @event)
    {
        this.Id = @event.WebhookId;
        this.Name = @event.Name;
        this.Url = @event.Url;
        this.HttpMethod = @event.HttpMethod;
        this.Secret = @event.Secret;
        this.EventTypes = @event.EventTypes;
        this.TenantId = @event.TenantId;
        this.Status = WebhookStatus.Active;
        this.DeliveryCount = 0;
        this.FailureCount = 0;
        this.CreatedAt = @event.Timestamp;
    }

    private void ApplyUpdated(WebhookUpdated @event)
    {
        this.Name = @event.Name;
        this.Url = @event.Url;
        this.HttpMethod = @event.HttpMethod;
        this.Secret = @event.Secret;
        this.EventTypes = @event.EventTypes;
    }

    private void ApplyActivated()
    {
        this.Status = WebhookStatus.Active;
    }

    private void ApplyDeactivated()
    {
        this.Status = WebhookStatus.Inactive;
    }

    private void ApplyDelivered()
    {
        this.DeliveryCount++;
        this.LastDeliveredAt = DateTime.UtcNow;
    }

    private void ApplyDeliveryFailed()
    {
        this.FailureCount++;
    }

    private void ApplyDeleted()
    {
        this.Status = WebhookStatus.Deleted;
    }
}

/// <summary>
/// Represents the status of a webhook.
/// </summary>
public enum WebhookStatus
{
    /// <summary>
    /// Webhook is active and receiving events.
    /// </summary>
    Active,

    /// <summary>
    /// Webhook is inactive and not receiving events.
    /// </summary>
    Inactive,

    /// <summary>
    /// Webhook has been deleted.
    /// </summary>
    Deleted,

    /// <summary>
    /// Webhook is failing repeatedly and may be disabled.
    /// </summary>
    Failing,
}
