// <copyright file="MicrosoftAgentAdapter.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Infrastructure.Adapters;

using System.Collections.Generic;
using System.Text.Json;
using Synaxis.Agents.Domain.Aggregates;
using Synaxis.Agents.Domain.ValueObjects;

/// <summary>
/// Adapter for agent framework integration with Synaxis domain models.
/// </summary>
public static class MicrosoftAgentAdapter
{
    /// <summary>
    /// Converts a Synaxis AgentConfiguration to a serializable dictionary.
    /// </summary>
    /// <param name="agentConfig">The Synaxis agent configuration.</param>
    /// <returns>A dictionary representation of the agent.</returns>
    public static IReadOnlyDictionary<string, object> ToSerializableAgent(AgentConfiguration agentConfig)
    {
        return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = agentConfig.Id,
            ["name"] = agentConfig.Name,
            ["description"] = agentConfig.Description ?? string.Empty,
            ["state"] = MapStatus(agentConfig.Status),
            ["version"] = agentConfig.Version,
            ["createdAt"] = agentConfig.CreatedAt,
            ["updatedAt"] = agentConfig.UpdatedAt,
        };
    }

    /// <summary>
    /// Converts a Synaxis AgentStep to a serializable dictionary.
    /// </summary>
    /// <param name="step">The Synaxis agent step.</param>
    /// <param name="context">The execution context.</param>
    /// <returns>A dictionary representation of the step.</returns>
    public static IReadOnlyDictionary<string, object?> ToSerializableStep(AgentStep step, IReadOnlyDictionary<string, object> context)
    {
        var stepDict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = step.Name,
            ["type"] = step.Type.ToString().ToLowerInvariant(),
            ["prompt"] = ReplaceVariables(step.Prompt, context),
            ["function"] = step.Function,
            ["arguments"] = step.Arguments,
            ["message"] = ReplaceVariables(step.Message, context),
            ["condition"] = step.Condition,
            ["collection"] = step.Collection,
            ["outputVariable"] = step.OutputVariable,
        };

        return stepDict;
    }

    /// <summary>
    /// Converts a step result to a context variable.
    /// </summary>
    /// <param name="result">The result of the step execution.</param>
    /// <param name="outputVariable">The variable name to store the result in.</param>
    /// <returns>A key-value pair representing the context variable.</returns>
    public static KeyValuePair<string, object>? ToContextVariable(object? result, string? outputVariable)
    {
        if (string.IsNullOrEmpty(outputVariable))
        {
            return null;
        }

        return new KeyValuePair<string, object>(outputVariable, result ?? string.Empty);
    }

    /// <summary>
    /// Maps Synaxis AgentStatus to a string representation.
    /// </summary>
    /// <param name="status">The Synaxis agent status.</param>
    /// <returns>A string representing the status.</returns>
    public static string MapStatus(AgentStatus status)
    {
        return status switch
        {
            AgentStatus.Idle => "idle",
            AgentStatus.Running => "running",
            AgentStatus.Paused => "paused",
            AgentStatus.Completed => "completed",
            AgentStatus.Failed => "failed",
            AgentStatus.Active => "active",
            AgentStatus.Inactive => "inactive",
            _ => "unknown",
        };
    }

    /// <summary>
    /// Maps a string to Synaxis AgentStatus.
    /// </summary>
    /// <param name="statusString">The string representation of the status.</param>
    /// <returns>The corresponding AgentStatus.</returns>
    public static AgentStatus MapStatus(string statusString)
    {
        return statusString.ToLowerInvariant() switch
        {
            "idle" => AgentStatus.Idle,
            "running" => AgentStatus.Running,
            "paused" => AgentStatus.Paused,
            "completed" => AgentStatus.Completed,
            "failed" => AgentStatus.Failed,
            "active" => AgentStatus.Active,
            "inactive" => AgentStatus.Inactive,
            _ => AgentStatus.Active,
        };
    }

    /// <summary>
    /// Maps a string to Synaxis AgentStepType.
    /// </summary>
    /// <param name="stepTypeString">The string representation of the step type.</param>
    /// <returns>The corresponding AgentStepType.</returns>
    public static AgentStepType MapStepType(string stepTypeString)
    {
        return stepTypeString.ToLowerInvariant() switch
        {
            "input" => AgentStepType.Input,
            "function" => AgentStepType.Function,
            "output" => AgentStepType.Output,
            "condition" => AgentStepType.Condition,
            "loop" => AgentStepType.Loop,
            _ => AgentStepType.Function,
        };
    }

    /// <summary>
    /// Serializes the agent execution context to JSON.
    /// </summary>
    /// <param name="context">The execution context.</param>
    /// <returns>A JSON string representation of the context.</returns>
    public static string SerializeContext(IReadOnlyDictionary<string, object> context)
    {
        return JsonSerializer.Serialize(context, new JsonSerializerOptions
        {
            WriteIndented = true,
        });
    }

    /// <summary>
    /// Deserializes a JSON string to an execution context.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A dictionary representing the execution context.</returns>
    public static IReadOnlyDictionary<string, object>? DeserializeContext(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<Dictionary<string, object>>(json);
    }

    private static string? ReplaceVariables(string? text, IReadOnlyDictionary<string, object> context)
    {
        if (string.IsNullOrEmpty(text) || context.Count == 0)
        {
            return text;
        }

        var result = text;
        foreach (var kvp in context)
        {
            result = result.Replace($"${{context.{kvp.Key}}}", kvp.Value?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }
}
