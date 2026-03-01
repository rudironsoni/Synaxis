// <copyright file="CommandBase.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.Commands;

/// <summary>
/// Base class for all commands.
/// </summary>
public abstract record CommandBase
{
    /// <summary>
    /// Gets the unique identifier for the command.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("commandId")]
    public Guid CommandId { get; init; }

    /// <summary>
    /// Gets the correlation identifier for tracing requests across services.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("correlationId")]
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// Gets the timestamp when the command was created.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets the identifier of the user issuing the command.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("userId")]
    public required string UserId { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandBase"/> class.
    /// </summary>
    protected CommandBase()
    {
        this.CommandId = Guid.NewGuid();
        this.CorrelationId = Guid.NewGuid();
        this.Timestamp = DateTimeOffset.UtcNow;
    }
}
