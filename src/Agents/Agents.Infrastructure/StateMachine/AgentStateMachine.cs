// <copyright file="AgentStateMachine.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Infrastructure.StateMachine;

using Synaxis.Agents.Domain.ValueObjects;

/// <summary>
/// Manages state transitions for agent execution.
/// </summary>
public static class AgentStateMachine
{
    private static readonly Dictionary<AgentStatus, HashSet<AgentStatus>> ValidTransitions = new()
    {
        [AgentStatus.Idle] = [AgentStatus.Running],
        [AgentStatus.Running] = [AgentStatus.Paused, AgentStatus.Completed, AgentStatus.Failed],
        [AgentStatus.Paused] = [AgentStatus.Running, AgentStatus.Failed],
        [AgentStatus.Completed] = [],
        [AgentStatus.Failed] = [],
        [AgentStatus.Active] = [AgentStatus.Inactive],
        [AgentStatus.Inactive] = [AgentStatus.Active],
    };

    /// <summary>
    /// Validates whether a state transition is allowed.
    /// </summary>
    /// <param name="currentState">The current state of the agent.</param>
    /// <param name="targetState">The target state to transition to.</param>
    /// <returns>True if the transition is valid; otherwise, false.</returns>
    public static bool CanTransition(AgentStatus currentState, AgentStatus targetState)
    {
        return ValidTransitions.TryGetValue(currentState, out var validStates)
            && validStates.Contains(targetState);
    }

    /// <summary>
    /// Gets the valid target states for a given current state.
    /// </summary>
    /// <param name="currentState">The current state of the agent.</param>
    /// <returns>A read-only list of valid target states.</returns>
    public static IReadOnlyList<AgentStatus> GetValidTransitions(AgentStatus currentState)
    {
        return ValidTransitions.TryGetValue(currentState, out var validStates)
            ? validStates.ToList().AsReadOnly()
            : [];
    }
}
