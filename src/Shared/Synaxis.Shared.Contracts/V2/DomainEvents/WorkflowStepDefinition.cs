// <copyright file="WorkflowStepDefinition.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Contracts.V2.DomainEvents;

/// <summary>
/// Defines a step within a workflow (V2).
/// </summary>
public record WorkflowStepDefinition
{
    /// <summary>
    /// Gets the unique identifier for the step.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the name of the step.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Gets the type of the step.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("stepType")]
    public required string StepType { get; init; }

    /// <summary>
    /// Gets the configuration for the step.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("configuration")]
    public IReadOnlyDictionary<string, object>? Configuration { get; init; }

    /// <summary>
    /// Gets the order of execution for this step.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("order")]
    public int Order { get; init; }

    /// <summary>
    /// Gets the condition for executing this step.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("condition")]
    public string? Condition { get; init; }

    /// <summary>
    /// Gets the retry policy for the step.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("retryPolicy")]
    public DomainEvents.RetryPolicy? RetryPolicy { get; init; }
}
