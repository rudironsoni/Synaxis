// <copyright file="AgentConverterExtensions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Contracts.V2.Converters;

using Synaxis.Shared.Contracts.V2.DTOs;

/// <summary>
/// Extension methods for agent conversion.
/// </summary>
public static class AgentConverterExtensions
{
    /// <summary>
    /// Converts a V1 AgentDto to V2 AgentDto.
    /// </summary>
    /// <param name="v1Agent">The V1 agent DTO.</param>
    /// <returns>The V2 agent DTO.</returns>
    public static AgentDto ToV2(this V1.DTOs.AgentDto v1Agent)
    {
        return new AgentConverter().Convert(v1Agent);
    }

    /// <summary>
    /// Converts a collection of V1 AgentDto to V2 AgentDto.
    /// </summary>
    /// <param name="v1Agents">The V1 agent DTOs.</param>
    /// <returns>The V2 agent DTOs.</returns>
    public static IEnumerable<AgentDto> ToV2(this IEnumerable<V1.DTOs.AgentDto> v1Agents)
    {
        var converter = new AgentConverter();
        return v1Agents.Select(converter.Convert);
    }
}
