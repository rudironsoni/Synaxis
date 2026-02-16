// <copyright file="ExecutionProgressed.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Domain.Events;

using Synaxis.Abstractions.Cloud;
using Synaxis.Agents.Domain.ValueObjects;

/// <summary>
/// Event raised when an agent execution progresses to a new step.
/// </summary>
public record ExecutionProgressed : IDomainEvent
{
    /// <inheritdoc/>
    public string EventId { get; init; } = Guid.NewGuid().ToString();

    /// <inheritdoc/>
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

    /// <inheritdoc/>
    public string EventType { get; init; } = nameof(ExecutionProgressed);

    /// <summary>
    /// Gets the unique identifier of the execution.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the current step number.
    /// </summary>
    public required int CurrentStep { get; init; }

    /// <summary>
    /// Gets the execution step details.
    /// </summary>
    public required ExecutionStep Step { get; init; }
}
