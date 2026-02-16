// <copyright file="AgentStep.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Domain.ValueObjects;

using System.Collections.Generic;

/// <summary>
/// Represents a single step in a declarative agent configuration.
/// </summary>
public sealed record AgentStep
{
    /// <summary>
    /// Gets the name of the step.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the type of the step.
    /// </summary>
    public required AgentStepType Type { get; init; }

    /// <summary>
    /// Gets the prompt to display to the user (for input steps).
    /// </summary>
    public string? Prompt { get; init; }

    /// <summary>
    /// Gets the function name to execute (for function steps).
    /// </summary>
    public string? Function { get; init; }

    /// <summary>
    /// Gets the arguments to pass to the function.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Arguments { get; init; }

    /// <summary>
    /// Gets the message to output (for output steps).
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Gets the condition to evaluate (for condition steps).
    /// </summary>
    public string? Condition { get; init; }

    /// <summary>
    /// Gets the collection to iterate over (for loop steps).
    /// </summary>
    public string? Collection { get; init; }

    /// <summary>
    /// Gets the variable name to store the result in.
    /// </summary>
    public string? OutputVariable { get; init; }
}
