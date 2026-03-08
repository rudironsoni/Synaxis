// <copyright file="ResourceRequirements.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Contracts.V2.Common;

/// <summary>
/// Resource requirements for an agent.
/// </summary>
public record ResourceRequirements
{
    /// <summary>
    /// Gets the CPU limit in millicores.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("cpu")]
    public string? Cpu { get; init; }

    /// <summary>
    /// Gets the memory limit (e.g., "512Mi", "1Gi").
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("memory")]
    public string? Memory { get; init; }

    /// <summary>
    /// Gets the GPU requirements.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("gpu")]
    public string? Gpu { get; init; }
}
