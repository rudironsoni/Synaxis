namespace Synaxis.Contracts.V1.Commands;

/// <summary>
/// Command to execute an agent.
/// </summary>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(ExecuteAgentCommand), "execute_agent")]
public record ExecuteAgentCommand : CommandBase
{
    /// <summary>
    /// Identifier of the agent to execute.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("agentId")]
    public required Guid TargetAgentId { get; init; }

    /// <summary>
    /// Input parameters for the execution.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("input")]
    public Dictionary<string, object>? Input { get; init; }

    /// <summary>
    /// Execution priority (higher = more urgent).
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("priority")]
    public int Priority { get; init; } = 0;

    /// <summary>
    /// Maximum allowed execution time.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("timeout")]
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Whether to wait for execution completion.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("waitForCompletion")]
    public bool WaitForCompletion { get; init; } = true;

    /// <summary>
    /// Callback URL for async notifications.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("callbackUrl")]
    public string? CallbackUrl { get; init; }
}
