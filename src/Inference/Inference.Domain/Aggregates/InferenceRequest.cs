// <copyright file="InferenceRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Aggregates;

using Synaxis.Infrastructure.EventSourcing;
using Synaxis.Inference.Domain.Events;

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

/// <summary>
/// Represents the status of an inference request.
/// </summary>
public enum InferenceStatus
{
    /// <summary>
    /// Request is pending routing.
    /// </summary>
    Pending,

    /// <summary>
    /// Request is being processed.
    /// </summary>
    Processing,

    /// <summary>
    /// Request completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Request failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Request was cancelled.
    /// </summary>
    Cancelled,
}

/// <summary>
/// Represents token usage for an inference request.
/// </summary>
public class TokenUsage
{
    /// <summary>
    /// Gets or sets the prompt tokens.
    /// </summary>
    public int PromptTokens { get; set; }

    /// <summary>
    /// Gets or sets the completion tokens.
    /// </summary>
    public int CompletionTokens { get; set; }

    /// <summary>
    /// Gets or sets the total tokens.
    /// </summary>
    public int TotalTokens => this.PromptTokens + this.CompletionTokens;
}

/// <summary>
/// Represents a routing decision.
/// </summary>
public class RoutingDecision
{
    /// <summary>
    /// Gets or sets the selected provider.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the selected model.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the routing reason.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the routing score.
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Gets or sets alternative options.
    /// </summary>
    public List<AlternativeOption> Alternatives { get; set; } = new();
}

/// <summary>
/// Represents an alternative routing option.
/// </summary>
public class AlternativeOption
{
    /// <summary>
    /// Gets or sets the provider.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the score.
    /// </summary>
    public double Score { get; set; }
}
