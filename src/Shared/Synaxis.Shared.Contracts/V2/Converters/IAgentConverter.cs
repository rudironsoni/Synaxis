// <copyright file="IAgentConverter.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Contracts.V2.Converters;

using Synaxis.Shared.Contracts.V2.DTOs;

/// <summary>
/// Converts V1 AgentDto to V2 AgentDto.
/// </summary>
public interface IAgentConverter
{
    /// <summary>
    /// Converts a V1 AgentDto to V2 AgentDto.
    /// </summary>
    /// <param name="v1Agent">The V1 agent DTO.</param>
    /// <returns>The V2 agent DTO.</returns>
    AgentDto Convert(V1.DTOs.AgentDto v1Agent);
}
