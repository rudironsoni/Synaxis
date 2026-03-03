// <copyright file="InferenceRequestEntity.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Infrastructure.Persistence;

using Synaxis.Inference.Domain.Aggregates;

/// <summary>
/// Entity representation of an inference request for persistence.
/// </summary>
public class InferenceRequestEntity
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Gets or sets the API key identifier.
    /// </summary>
    public Guid? ApiKeyId { get; set; }

    /// <summary>
    /// Gets or sets the model identifier.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the provider identifier.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the request status.
    /// </summary>
    public InferenceStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the request content.
    /// </summary>
    public string RequestContent { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the response content.
    /// </summary>
    public string? ResponseContent { get; set; }

    /// <summary>
    /// Gets or sets the error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the token usage.
    /// </summary>
    public TokenUsage TokenUsage { get; set; } = new();

    /// <summary>
    /// Gets or sets the cost.
    /// </summary>
    public decimal Cost { get; set; }

    /// <summary>
    /// Gets or sets the latency in milliseconds.
    /// </summary>
    public long LatencyMs { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the completed timestamp.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Converts the entity to a domain aggregate.
    /// </summary>
    /// <returns>The domain aggregate.</returns>
    public InferenceRequest ToDomain()
    {
        return InferenceRequest.Create(
            this.Id,
            this.TenantId,
            this.UserId,
            this.ApiKeyId,
            this.ModelId,
            this.RequestContent);
    }

    /// <summary>
    /// Creates an entity from a domain aggregate.
    /// </summary>
    /// <param name="request">The domain aggregate.</param>
    /// <returns>The entity.</returns>
    public static InferenceRequestEntity FromDomain(InferenceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new InferenceRequestEntity
        {
            Id = request.Id,
            TenantId = request.TenantId,
            UserId = request.UserId,
            ApiKeyId = request.ApiKeyId,
            ModelId = request.ModelId,
            ProviderId = request.ProviderId,
            Status = request.Status,
            RequestContent = request.RequestContent,
            ResponseContent = request.ResponseContent,
            ErrorMessage = request.ErrorMessage,
            TokenUsage = request.TokenUsage,
            Cost = request.Cost,
            LatencyMs = request.LatencyMs,
            CreatedAt = request.CreatedAt,
            CompletedAt = request.CompletedAt,
        };
    }
}
