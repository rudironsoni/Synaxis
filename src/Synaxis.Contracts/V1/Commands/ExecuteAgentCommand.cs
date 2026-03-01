// <copyright file="ExecuteAgentCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.Commands;

/// <summary>
/// Command to execute an agent.
/// </summary>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(ExecuteAgentCommand), "execute_agent")]
public record ExecuteAgentCommand : CommandBase
{
    /// <summary>
    /// Gets the identifier of the agent to execute.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("agentId")]
    public required Guid TargetAgentId { get; init; }

    /// <summary>
    /// Gets the input parameters for the execution.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("input")]
    public IReadOnlyDictionary<string, object>? Input { get; init; }

    /// <summary>
    /// Gets the execution priority (higher = more urgent).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("priority")]
    public int Priority { get; init; } = 0;

    /// <summary>
    /// Gets the maximum allowed execution time.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("timeout")]
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Gets a value indicating whether to wait for execution completion.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("waitForCompletion")]
    public bool WaitForCompletion { get; init; } = true;

    /// <summary>
    /// Gets the callback URL for async notifications.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("callbackUrl")]
    public string? CallbackUrl { get; init; }
}
