// <copyright file="FailedStepInfo.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V2.DomainEvents;

using Synaxis.Contracts.V2.Common;

/// <summary>
/// Information about a failed step.
/// </summary>
public record FailedStepInfo
{
    /// <summary>
    /// Gets the identifier of the failed step.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("stepId")]
    public required Guid StepId { get; init; }

    /// <summary>
    /// Gets the name of the failed step.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("stepName")]
    public required string StepName { get; init; }

    /// <summary>
    /// Gets the error details.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("error")]
    public required ExecutionError Error { get; init; }
}
