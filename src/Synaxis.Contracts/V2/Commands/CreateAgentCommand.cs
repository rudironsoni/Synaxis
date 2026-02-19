namespace Synaxis.Contracts.V2.Commands;

/// <summary>
/// Command to create a new agent (V2).
/// </summary>
/// <remarks>
/// V2 Breaking Changes:
/// - Added ResourceRequirements for scheduling
/// - Added Labels for Kubernetes-style tagging
/// - Tags renamed to Labels
/// </remarks>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(CreateAgentCommand), "create_agent")]
public record CreateAgentCommand : CommandBase
{
    /// <summary>
    /// Name of the agent.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Description of the agent.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>
    /// Type of the agent.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("agentType")]
    public required string AgentType { get; init; }

    /// <summary>
    /// Configuration settings for the agent.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("configuration")]
    public Dictionary<string, object>? Configuration { get; init; }

    /// <summary>
    /// Resource requirements for the agent.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("resources")]
    public ResourceRequirements? Resources { get; init; }

    /// <summary>
    /// Labels for the agent.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("labels")]
    public Dictionary<string, string>? Labels { get; init; }
}

/// <summary>
/// Resource requirements for an agent.
/// </summary>
public record ResourceRequirements
{
    /// <summary>
    /// CPU limit in millicores.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("cpu")]
    public string? Cpu { get; init; }

    /// <summary>
    /// Memory limit (e.g., "512Mi", "1Gi").
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("memory")]
    public string? Memory { get; init; }

    /// <summary>
    /// GPU requirements.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("gpu")]
    public string? Gpu { get; init; }
}
