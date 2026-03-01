// <copyright file="CancelAgentExecutionCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.Commands;

/// <summary>
/// Command to cancel an ongoing agent execution.
/// </summary>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(CancelAgentExecutionCommand), "cancel_agent_execution")]
public record CancelAgentExecutionCommand : CommandBase
{
    /// <summary>
    /// Gets the identifier of the execution to cancel.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("executionId")]
    public required Guid ExecutionId { get; init; }

    /// <summary>
    /// Gets the reason for cancellation.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("reason")]
    public string? Reason { get; init; }

    /// <summary>
    /// Gets a value indicating whether to force immediate termination.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("force")]
    public bool Force { get; init; }
}
