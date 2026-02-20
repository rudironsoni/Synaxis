// <copyright file="WorkflowCreated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Domain.Events;

using Mediator;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// Event raised when a new agent workflow is created.
/// </summary>
public record WorkflowCreated : IDomainEvent, INotification
{
    /// <inheritdoc/>
    public string EventId { get; init; } = Guid.NewGuid().ToString();

    /// <inheritdoc/>
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

    /// <inheritdoc/>
    public string EventType { get; init; } = nameof(WorkflowCreated);

    /// <summary>
    /// Gets the unique identifier of the workflow.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the name of the workflow.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the description of the workflow.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the YAML configuration for the workflow.
    /// </summary>
    public required string WorkflowYaml { get; init; }

    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public required string TenantId { get; init; }

    /// <summary>
    /// Gets the team identifier.
    /// </summary>
    public string? TeamId { get; init; }

    /// <summary>
    /// Gets the version of the workflow.
    /// </summary>
    public required int Version { get; init; }
}
