// <copyright file="WorkflowVariable.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Contracts.V2.DTOs;

/// <summary>
/// Workflow variable definition.
/// </summary>
public record WorkflowVariable
{
    /// <summary>
    /// Gets the variable name.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Gets the variable type.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// Gets the default value.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("defaultValue")]
    public object? DefaultValue { get; init; }

    /// <summary>
    /// Gets a value indicating whether the variable is required.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("required")]
    public bool Required { get; init; }
}
