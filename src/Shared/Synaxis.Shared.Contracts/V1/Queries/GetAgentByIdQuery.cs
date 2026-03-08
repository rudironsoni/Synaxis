// <copyright file="GetAgentByIdQuery.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Contracts.V1.Queries;

/// <summary>
/// Query to get an agent by its identifier.
/// </summary>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(GetAgentByIdQuery), "get_agent_by_id")]
public record GetAgentByIdQuery : QueryBase
{
    /// <summary>
    /// Gets the identifier of the agent to retrieve.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("targetAgentId")]
    public required Guid TargetAgentId { get; init; }

    /// <summary>
    /// Gets a value indicating whether to include configuration details.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("includeConfiguration")]
    public bool IncludeConfiguration { get; init; } = true;
}
