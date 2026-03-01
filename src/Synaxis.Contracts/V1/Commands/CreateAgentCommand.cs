// <copyright file="CreateAgentCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.Commands;

/// <summary>
/// Command to create a new agent.
/// </summary>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(CreateAgentCommand), "create_agent")]
public record CreateAgentCommand : CommandBase
{
    /// <summary>
    /// Gets the name of the agent.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Gets the description of the agent.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>
    /// Gets the type of the agent (e.g., chat, embedding, etc.).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("agentType")]
    public required string AgentType { get; init; }

    /// <summary>
    /// Gets the configuration settings for the agent.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("configuration")]
    public IReadOnlyDictionary<string, object>? Configuration { get; init; }

    /// <summary>
    /// Gets the tags for categorizing the agent.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("tags")]
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}
