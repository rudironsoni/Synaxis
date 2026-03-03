// <copyright file="AgentParameter.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Domain.ValueObjects;

/// <summary>
/// Represents a parameter definition for an agent configuration.
/// </summary>
public record AgentParameter
{
    /// <summary>
    /// Gets the name of the parameter.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the type of the parameter.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets the default value for the parameter.
    /// </summary>
    public string? DefaultValue { get; init; }

    /// <summary>
    /// Gets a value indicating whether the parameter is required.
    /// </summary>
    public required bool Required { get; init; }
}
