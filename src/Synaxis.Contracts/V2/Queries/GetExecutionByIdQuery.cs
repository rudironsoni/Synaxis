namespace Synaxis.Contracts.V2.Queries;

/// <summary>
/// Query to get an execution by its identifier (V2).
/// </summary>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(GetExecutionByIdQuery), "get_execution_by_id")]
public record GetExecutionByIdQuery : QueryBase
{
    /// <summary>
    /// Identifier of the execution to retrieve.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("executionId")]
    public required Guid TargetExecutionId { get; init; }

    /// <summary>
    /// Whether to include detailed logs.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("includeLogs")]
    public bool IncludeLogs { get; init; }

    /// <summary>
    /// Whether to include input/output data.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("includeData")]
    public bool IncludeData { get; init; } = true;

    /// <summary>
    /// Whether to include execution metrics.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("includeMetrics")]
    public bool IncludeMetrics { get; init; } = true;

    /// <summary>
    /// Whether to include execution stages.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("includeStages")]
    public bool IncludeStages { get; init; }
}
