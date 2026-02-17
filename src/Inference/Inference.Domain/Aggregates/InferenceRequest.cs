// <copyright file="InferenceRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Aggregates;

using Synaxis.Inference.Domain.Events;
using Synaxis.Infrastructure.EventSourcing;

/// <summary>
/// Aggregate root representing an AI inference request.
/// </summary>
public class InferenceRequest : AggregateRoot
{
    /// <summary>
    /// Gets the request identifier.
    /// </summary>
    public new Guid Id { get; private set; }

    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Gets the user identifier.
    /// </summary>
    public Guid? UserId { get; private set; }

    /// <summary>
    /// Gets the API key identifier.
    /// </summary>
    public Guid? ApiKeyId { get; private set; }

    /// <summary>
    /// Gets the model identifier.
    /// </summary>
    public string ModelId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the provider identifier.
    /// </summary>
    public string ProviderId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the request content.
    /// </summary>
    public string RequestContent { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the response content.
    /// </summary>
    public string? ResponseContent { get; private set; }

    /// <summary>
    /// Gets the request status.
    /// </summary>
    public InferenceStatus Status { get; private set; }

    /// <summary>
    /// Gets the token usage.
    /// </summary>
    public TokenUsage TokenUsage { get; private set; } = new();

    /// <summary>
    /// Gets the cost.
    /// </summary>
    public decimal Cost { get; private set; }

    /// <summary>
    /// Gets the latency in milliseconds.
    /// </summary>
    public long LatencyMs { get; private set; }

    /// <summary>
    /// Gets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the completed timestamp.
    /// </summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// Gets the error message if failed.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the routing decision.
    /// </summary>
    public RoutingDecision? RoutingDecision { get; private set; }

    /// <summary>
    /// Creates a new inference request.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="apiKeyId">The API key identifier.</param>
    /// <param name="modelId">The model identifier.</param>
    /// <param name="requestContent">The request content.</param>
    /// <param name="routingDecision">The routing decision.</param>
    /// <returns>A new inference request instance.</returns>
    public static InferenceRequest Create(
        Guid id,
        Guid tenantId,
        Guid? userId,
        Guid? apiKeyId,
        string modelId,
        string requestContent,
        RoutingDecision? routingDecision = null)
    {
        var request = new InferenceRequest();
        var @event = new InferenceRequestCreated
        {
            RequestId = id,
            TenantId = tenantId,
            UserId = userId,
            ApiKeyId = apiKeyId,
            ModelId = modelId,
            RequestContent = requestContent,
            RoutingDecision = routingDecision,
            Timestamp = DateTime.UtcNow,
        };

        request.ApplyEvent(@event);
        return request;
    }

    /// <summary>
    /// Routes the request to a provider.
    /// </summary>
    /// <param name="providerId">The provider identifier.</param>
    /// <param name="modelId">The model identifier.</param>
    /// <param name="decision">The routing decision.</param>
    public void Route(string providerId, string modelId, RoutingDecision decision)
    {
        var @event = new InferenceRequestRouted
        {
            RequestId = this.Id,
            ProviderId = providerId,
            ModelId = modelId,
            RoutingDecision = decision,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Starts processing the request.
    /// </summary>
    public void StartProcessing()
    {
        if (this.Status != InferenceStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot start processing in {this.Status} status.");
        }

        var @event = new InferenceProcessingStarted
        {
            RequestId = this.Id,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Completes the request successfully.
    /// </summary>
    /// <param name="responseContent">The response content.</param>
    /// <param name="tokenUsage">The token usage.</param>
    /// <param name="cost">The cost.</param>
    /// <param name="latencyMs">The latency in milliseconds.</param>
    public void Complete(string responseContent, TokenUsage tokenUsage, decimal cost, long latencyMs)
    {
        if (this.Status != InferenceStatus.Processing)
        {
            throw new InvalidOperationException($"Cannot complete in {this.Status} status.");
        }

        var @event = new InferenceRequestCompleted
        {
            RequestId = this.Id,
            ResponseContent = responseContent,
            TokenUsage = tokenUsage,
            Cost = cost,
            LatencyMs = latencyMs,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Fails the request.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    public void Fail(string errorMessage)
    {
        if (this.Status != InferenceStatus.Pending && this.Status != InferenceStatus.Processing)
        {
            throw new InvalidOperationException($"Cannot fail in {this.Status} status.");
        }

        var @event = new InferenceRequestFailed
        {
            RequestId = this.Id,
            ErrorMessage = errorMessage,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Retries the request.
    /// </summary>
    /// <param name="newProviderId">The new provider identifier.</param>
    public void Retry(string newProviderId)
    {
        if (this.Status != InferenceStatus.Failed)
        {
            throw new InvalidOperationException($"Cannot retry in {this.Status} status.");
        }

        var @event = new InferenceRequestRetried
        {
            RequestId = this.Id,
            NewProviderId = newProviderId,
            Timestamp = DateTime.UtcNow,
        };

        this.ApplyEvent(@event);
    }

    /// <inheritdoc/>
    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case InferenceRequestCreated created:
                this.ApplyCreated(created);
                break;
            case InferenceRequestRouted routed:
                this.ApplyRouted(routed);
                break;
            case InferenceProcessingStarted:
                this.ApplyProcessingStarted();
                break;
            case InferenceRequestCompleted completed:
                this.ApplyCompleted(completed);
                break;
            case InferenceRequestFailed failed:
                this.ApplyFailed(failed);
                break;
            case InferenceRequestRetried:
                this.ApplyRetried();
                break;
        }
    }

    private void ApplyCreated(InferenceRequestCreated @event)
    {
        this.Id = @event.RequestId;
        this.TenantId = @event.TenantId;
        this.UserId = @event.UserId;
        this.ApiKeyId = @event.ApiKeyId;
        this.ModelId = @event.ModelId;
        this.RequestContent = @event.RequestContent;
        this.RoutingDecision = @event.RoutingDecision;
        this.Status = InferenceStatus.Pending;
        this.CreatedAt = @event.Timestamp;
    }

    private void ApplyRouted(InferenceRequestRouted @event)
    {
        this.ProviderId = @event.ProviderId;
        this.ModelId = @event.ModelId;
        this.RoutingDecision = @event.RoutingDecision;
    }

    private void ApplyProcessingStarted()
    {
        this.Status = InferenceStatus.Processing;
    }

    private void ApplyCompleted(InferenceRequestCompleted @event)
    {
        this.Status = InferenceStatus.Completed;
        this.ResponseContent = @event.ResponseContent;
        this.TokenUsage = @event.TokenUsage;
        this.Cost = @event.Cost;
        this.LatencyMs = @event.LatencyMs;
        this.CompletedAt = DateTime.UtcNow;
    }

    private void ApplyFailed(InferenceRequestFailed @event)
    {
        this.Status = InferenceStatus.Failed;
        this.ErrorMessage = @event.ErrorMessage;
        this.CompletedAt = DateTime.UtcNow;
    }

    private void ApplyRetried()
    {
        this.Status = InferenceStatus.Pending;
        this.ErrorMessage = null;
        this.CompletedAt = null;
    }
}
