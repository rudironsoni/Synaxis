// <copyright file="InferenceRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Entities;

/// <summary>
/// Represents an inference request.
/// </summary>
public class InferenceRequest
{
    /// <summary>
    /// Gets or sets the request identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the status.
    /// </summary>
    public InferenceStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the model identifier.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the provider identifier.
    /// </summary>
    public string? ProviderId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether streaming is enabled.
    /// </summary>
    public bool EnableStreaming { get; set; }

    /// <summary>
    /// Gets or sets the created timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the completed timestamp.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the retry count.
    /// </summary>
    public int RetryCount { get; set; }
}

/// <summary>
/// Represents the status of an inference request.
/// </summary>
public enum InferenceStatus
{
    /// <summary>
    /// The request is pending.
    /// </summary>
    Pending,

    /// <summary>
    /// The request is being routed.
    /// </summary>
    Routing,

    /// <summary>
    /// The request is executing.
    /// </summary>
    Executing,

    /// <summary>
    /// The request completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// The request failed.
    /// </summary>
    Failed,

    /// <summary>
    /// The request is being retried.
    /// </summary>
    Retrying,
}
