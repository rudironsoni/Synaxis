// <copyright file="AgentStepType.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Domain.ValueObjects;

/// <summary>
/// Represents the type of an agent step in a declarative agent configuration.
/// </summary>
public enum AgentStepType
{
    /// <summary>
    /// A step that collects input from the user.
    /// </summary>
    Input,

    /// <summary>
    /// A step that executes a function or tool.
    /// </summary>
    Function,

    /// <summary>
    /// A step that outputs a result to the user.
    /// </summary>
    Output,

    /// <summary>
    /// A step that performs a conditional check.
    /// </summary>
    Condition,

    /// <summary>
    /// A step that iterates over a collection.
    /// </summary>
    Loop,
}
