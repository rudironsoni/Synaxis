namespace Synaxis.Contracts.V1.Commands;

/// <summary>
/// Base class for all commands.
/// </summary>
public abstract record CommandBase
{
    /// <summary>
    /// Unique identifier for the command.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("commandId")]
    public Guid CommandId { get; init; }

    /// <summary>
    /// Correlation identifier for tracing requests across services.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("correlationId")]
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// Timestamp when the command was created.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Identifier of the user issuing the command.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("userId")]
    public required string UserId { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandBase"/> class.
    /// </summary>
    protected CommandBase()
    {
        CommandId = Guid.NewGuid();
        CorrelationId = Guid.NewGuid();
        Timestamp = DateTimeOffset.UtcNow;
    }
}
