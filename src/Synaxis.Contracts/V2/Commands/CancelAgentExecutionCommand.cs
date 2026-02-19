namespace Synaxis.Contracts.V2.Commands;

/// <summary>
/// Command to cancel an ongoing agent execution (V2).
/// </summary>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(CancelAgentExecutionCommand), "cancel_agent_execution")]
public record CancelAgentExecutionCommand : CommandBase
{
    /// <summary>
    /// Identifier of the execution to cancel.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("executionId")]
    public required Guid ExecutionId { get; init; }

    /// <summary>
    /// Reason for cancellation.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("reason")]
    public string? Reason { get; init; }

    /// <summary>
    /// Whether to force immediate termination.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("force")]
    public bool Force { get; init; }

    /// <summary>
    /// Whether to wait for graceful shutdown.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("waitForGracefulShutdown")]
    public bool WaitForGracefulShutdown { get; init; } = true;
}
