// <copyright file="ResourceRequirements.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.DTOs;

/// <summary>
/// Resource requirements for an agent.
/// </summary>
public record ResourceRequirements
{
    /// <summary>
    /// Gets CPU limit in millicores.
    /// </summary>
    public string? Cpu { get; init; }

    /// <summary>
    /// Gets memory limit (e.g., "512Mi", "1Gi").
    /// </summary>
    public string? Memory { get; init; }

    /// <summary>
    /// Gets GPU requirements.
    /// </summary>
    public string? Gpu { get; init; }
}
