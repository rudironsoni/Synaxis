// <copyright file="SagaFailed.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Domain.Events;

using MediatR;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when a saga fails.
/// </summary>
public class SagaFailed : DomainEvent, INotification
{
    /// <summary>
    /// Gets or sets the saga identifier.
    /// </summary>
    public Guid SagaId { get; set; }

    /// <summary>
    /// Gets or sets the failed activity identifier.
    /// </summary>
    public Guid FailedActivityId { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.SagaId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(SagaFailed);
}
