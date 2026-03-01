// <copyright file="DeleteAgentCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.Commands;

/// <summary>
/// Command to delete an agent.
/// </summary>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(DeleteAgentCommand), "delete_agent")]
public record DeleteAgentCommand : CommandBase
{
    /// <summary>
    /// Gets the identifier of the agent to delete.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("agentId")]
    public required Guid TargetAgentId { get; init; }

    /// <summary>
    /// Gets the reason for deletion.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("reason")]
    public string? Reason { get; init; }

    /// <summary>
    /// Gets a value indicating whether to force deletion even if the agent has execution history.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("force")]
    public bool Force { get; init; }
}
