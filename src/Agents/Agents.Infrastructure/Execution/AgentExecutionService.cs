// <copyright file="AgentExecutionService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Infrastructure.Execution;

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Synaxis.Agents.Domain.Aggregates;
using Synaxis.Agents.Domain.ValueObjects;

/// <summary>
/// Executes agents using declarative step-based configuration.
/// </summary>
public sealed class AgentExecutionService
{
    private readonly ConcurrentDictionary<string, Func<string, Task<object>>> _functionRegistry;
    private readonly ILogger<AgentExecutionService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentExecutionService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public AgentExecutionService(ILogger<AgentExecutionService> logger)
    {
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this._functionRegistry = new ConcurrentDictionary<string, Func<string, Task<object>>>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Registers a function that can be called by agents.
    /// </summary>
    /// <param name="functionName">The name of the function.</param>
    /// <param name="function">The function implementation.</param>
    public void RegisterFunction(string functionName, Func<string, Task<object>> function)
    {
        if (string.IsNullOrWhiteSpace(functionName))
        {
            throw new ArgumentNullException(nameof(functionName));
        }

        this._functionRegistry.TryAdd(functionName, function);
        this._logger.LogInformation("Registered function: {FunctionName}", functionName);
    }

    /// <summary>
    /// Executes an agent step.
    /// </summary>
    /// <param name="execution">The agent execution instance.</param>
    /// <param name="step">The step to execute.</param>
    /// <param name="context">The execution context containing variables.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation with the step result.</returns>
    public async Task<object?> ExecuteStepAsync(
        AgentExecution execution,
        AgentStep step,
        IReadOnlyDictionary<string, object> context,
        CancellationToken cancellationToken = default)
    {
        this._logger.LogInformation(
            "Executing step {StepName} of type {StepType} for execution {ExecutionId}",
            step.Name,
            step.Type,
            execution.ExecutionId);

        try
        {
            var result = await this.ExecuteStepByTypeAsync(step, context).ConfigureAwait(false);
            this.RecordStepProgress(execution, step);
            this._logger.LogInformation(
                "Step {StepName} completed successfully for execution {ExecutionId}",
                step.Name,
                execution.ExecutionId);

            return result;
        }
        catch (Exception ex)
        {
            this._logger.LogError(
                ex,
                "Step {StepName} failed for execution {ExecutionId}: {ErrorMessage}",
                step.Name,
                execution.ExecutionId,
                ex.Message);
            execution.Fail(ex.Message);
            throw new InvalidOperationException($"Step {step.Name} failed: {ex.Message}", ex);
        }
    }

    private Task<object?> ExecuteStepByTypeAsync(
        AgentStep step,
        IReadOnlyDictionary<string, object> context)
    {
        return step.Type switch
        {
            AgentStepType.Input => ExecuteInputStepAsync(step, context),
            AgentStepType.Function => this.ExecuteFunctionStepAsync(step),
            AgentStepType.Output => ExecuteOutputStepAsync(step, context),
            AgentStepType.Condition => ExecuteConditionStepAsync(step, context),
            AgentStepType.Loop => ExecuteLoopStepAsync(step, context),
            _ => throw new InvalidOperationException($"Unknown step type: {step.Type}"),
        };
    }

    private void RecordStepProgress(AgentExecution execution, AgentStep step)
    {
        var executionStep = new ExecutionStep
        {
            StepNumber = execution.CurrentStep + 1,
            Name = step.Name,
            Status = AgentStatus.Completed,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
        };

        execution.Progress(executionStep);
    }

    /// <summary>
    /// Executes all steps of an agent.
    /// </summary>
    /// <param name="execution">The agent execution instance.</param>
    /// <param name="steps">The steps to execute.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ExecuteAgentAsync(
        AgentExecution execution,
        IReadOnlyList<AgentStep> steps,
        CancellationToken cancellationToken = default)
    {
        this._logger.LogInformation("Starting execution of agent {AgentId}", execution.AgentId);

        var context = new Dictionary<string, object>(execution.InputParameters, StringComparer.OrdinalIgnoreCase);

        while (execution.Status == AgentStatus.Running && execution.CurrentStep < steps.Count)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var step = steps[execution.CurrentStep];
            var result = await this.ExecuteStepAsync(execution, step, context, cancellationToken).ConfigureAwait(false);

            if (step.OutputVariable != null && result != null)
            {
                context[step.OutputVariable] = result;
            }
        }

        if (execution.Status == AgentStatus.Running)
        {
            execution.Complete();
            this._logger.LogInformation("Execution {ExecutionId} completed successfully", execution.ExecutionId);
        }
    }

    private static Task<object?> ExecuteInputStepAsync(
        AgentStep step,
        IReadOnlyDictionary<string, object> context)
    {
        if (string.IsNullOrEmpty(step.Prompt))
        {
            throw new InvalidOperationException("Input step must have a prompt");
        }

        var prompt = ReplaceVariables(step.Prompt, context);
        return Task.FromResult<object?>(prompt);
    }

    private Task<object?> ExecuteFunctionStepAsync(AgentStep step)
    {
        if (string.IsNullOrEmpty(step.Function))
        {
            throw new InvalidOperationException("Function step must have a function name");
        }

        if (!this._functionRegistry.TryGetValue(step.Function, out var function))
        {
            throw new InvalidOperationException($"Function not registered: {step.Function}");
        }

        var argumentsJson = step.Arguments != null
            ? System.Text.Json.JsonSerializer.Serialize(step.Arguments)
            : string.Empty;

        // Task<object> is covariant with Task<object?> at runtime
        return function(argumentsJson)!;
    }

    private static Task<object?> ExecuteOutputStepAsync(
        AgentStep step,
        IReadOnlyDictionary<string, object> context)
    {
        if (string.IsNullOrEmpty(step.Message))
        {
            throw new InvalidOperationException("Output step must have a message");
        }

        var message = ReplaceVariables(step.Message, context);
        return Task.FromResult<object?>(message);
    }

    private static Task<object?> ExecuteConditionStepAsync(
        AgentStep step,
        IReadOnlyDictionary<string, object> context)
    {
        if (string.IsNullOrEmpty(step.Condition))
        {
            throw new InvalidOperationException("Condition step must have a condition");
        }

        var condition = ReplaceVariables(step.Condition, context);
        var result = EvaluateCondition(condition);
        return Task.FromResult<object?>(result);
    }

    private static Task<object?> ExecuteLoopStepAsync(
        AgentStep step,
        IReadOnlyDictionary<string, object> context)
    {
        if (string.IsNullOrEmpty(step.Collection))
        {
            throw new InvalidOperationException("Loop step must have a collection");
        }

        if (!context.TryGetValue(step.Collection, out var collection))
        {
            throw new InvalidOperationException($"Collection '{step.Collection}' not found in context");
        }

        if (collection is not System.Collections.IEnumerable enumerable)
        {
            throw new InvalidOperationException($"Collection '{step.Collection}' is not enumerable");
        }

        var items = enumerable.Cast<object>().ToList();
        return Task.FromResult<object?>(items);
    }

    private static string ReplaceVariables(string text, IReadOnlyDictionary<string, object> context)
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

    private static bool EvaluateCondition(string condition)
    {
        if (bool.TryParse(condition, out var result))
        {
            return result;
        }

        return !string.IsNullOrWhiteSpace(condition);
    }
}
