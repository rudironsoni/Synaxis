// <copyright file="UpdateAgentCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V2.Commands;

using Synaxis.Contracts.V2.Common;

/// <summary>
/// Command to update an existing agent (V2).
/// </summary>
/// <remarks>
/// V2 Breaking Changes:
/// - Uses UpdateMask for partial updates
/// - Tags renamed to Labels.
/// </remarks>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(UpdateAgentCommand), "update_agent")]
public record UpdateAgentCommand : CommandBase
{
    /// <summary>
    /// Gets the identifier of the agent to update.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("agentId")]
    public required Guid TargetAgentId { get; init; }

    /// <summary>
    /// Gets the fields to update.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("updateMask")]
    public required IReadOnlyList<string> UpdateMask { get; init; }

    /// <summary>
    /// Gets the updated name.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>
    /// Gets the updated description.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>
    /// Gets the updated configuration.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("configuration")]
    public IReadOnlyDictionary<string, object>? Configuration { get; init; }

    /// <summary>
    /// Gets the updated resource requirements.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("resources")]
    public ResourceRequirements? Resources { get; init; }

    /// <summary>
    /// Gets the updated labels.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("labels")]
    public IReadOnlyDictionary<string, string>? Labels { get; init; }

    /// <summary>
    /// Gets the updated status.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public string? Status { get; init; }
}
