// <copyright file="InferenceRequestRetried.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Events;

using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when an inference request is retried.
/// </summary>
public class InferenceRequestRetried : DomainEvent
{
    /// <summary>
    /// Gets or sets the request identifier.
    /// </summary>
    public Guid RequestId { get; set; }

    /// <summary>
    /// Gets or sets the new provider identifier.
    /// </summary>
    public string NewProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.RequestId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(InferenceRequestRetried);
}
