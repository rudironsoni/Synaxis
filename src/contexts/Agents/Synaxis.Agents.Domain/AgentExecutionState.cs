// <copyright file="AgentExecutionState.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Domain;

/// <summary>
/// Represents the execution state of an agent.
/// </summary>
public enum AgentExecutionState
{
    /// <summary>
    /// The agent is idle and ready to start execution.
    /// </summary>
    Idle = 0,

    /// <summary>
    /// The agent is currently executing.
    /// </summary>
    Running = 1,

    /// <summary>
    /// The agent execution is paused.
    /// </summary>
    Paused = 2,

    /// <summary>
    /// The agent execution has completed successfully.
    /// </summary>
    Completed = 3,

    /// <summary>
    /// The agent execution has failed.
    /// </summary>
    Failed = 4,

    /// <summary>
    /// The agent execution has been cancelled.
    /// </summary>
    Cancelled = 5,
}
