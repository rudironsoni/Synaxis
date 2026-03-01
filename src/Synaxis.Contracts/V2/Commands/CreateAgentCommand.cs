// <copyright file="CreateAgentCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V2.Commands;

using Synaxis.Contracts.V2.Common;

/// <summary>
/// Command to create a new agent (V2).
/// </summary>
/// <remarks>
/// V2 Breaking Changes:
/// - Added ResourceRequirements for scheduling
/// - Added Labels for Kubernetes-style tagging
/// - Tags renamed to Labels.
/// </remarks>
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
    /// Gets the type of the agent.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("agentType")]
    public required string AgentType { get; init; }

    /// <summary>
    /// Gets the configuration settings for the agent.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("configuration")]
    public IReadOnlyDictionary<string, object>? Configuration { get; init; }

    /// <summary>
    /// Gets the resource requirements for the agent.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("resources")]
    public ResourceRequirements? Resources { get; init; }

    /// <summary>
    /// Gets the labels for the agent.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("labels")]
    public IReadOnlyDictionary<string, string>? Labels { get; init; }
}
