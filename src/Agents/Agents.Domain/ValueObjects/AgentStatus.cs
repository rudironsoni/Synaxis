// <copyright file="AgentStatus.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Domain.ValueObjects;

/// <summary>
/// Represents the current status of an agent.
/// </summary>
public enum AgentStatus
{
    /// <summary>
    /// The agent is idle and ready to accept new tasks.
    /// </summary>
    Idle = 0,

    /// <summary>
    /// The agent is currently executing a task.
    /// </summary>
    Running = 1,

    /// <summary>
    /// The agent execution is paused.
    /// </summary>
    Paused = 2,

    /// <summary>
    /// The agent has completed its task successfully.
    /// </summary>
    Completed = 3,

    /// <summary>
    /// The agent execution has failed.
    /// </summary>
    Failed = 4,

    /// <summary>
    /// The agent is active and available for execution.
    /// </summary>
    Active = 5,

    /// <summary>
    /// The agent is inactive and not available for execution.
    /// </summary>
    Inactive = 6,
}
