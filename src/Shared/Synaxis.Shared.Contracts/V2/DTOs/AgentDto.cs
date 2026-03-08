// <copyright file="AgentDto.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Contracts.V2.DTOs;

using Synaxis.Shared.Contracts.V2.Common;

/// <summary>
/// Data transfer object for an agent (V2).
/// </summary>
/// <remarks>
/// V2 Breaking Changes:
/// - Added ResourceRequirements
/// - Tags renamed to Labels
/// - Added ExecutionStats.
/// </remarks>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(AgentDto), "agent")]
public record AgentDto
{
    /// <summary>
    /// Gets the unique identifier of the agent.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public required Guid Id { get; init; }

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
    /// Gets the current status of the agent.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public required AgentStatus Status { get; init; }

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

    /// <summary>
    /// Gets the identifier of the user who created the agent.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("createdByUserId")]
    public required string CreatedByUserId { get; init; }

    /// <summary>
    /// Gets the timestamp when the agent was created.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets the timestamp when the agent was last updated.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }

    /// <summary>
    /// Gets the execution statistics.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("stats")]
    public ExecutionStats? Stats { get; init; }
}
