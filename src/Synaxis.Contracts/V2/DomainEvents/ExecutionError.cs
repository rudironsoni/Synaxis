// <copyright file="ExecutionError.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V2.DomainEvents;

/// <summary>
/// Execution error details.
/// </summary>
public record ExecutionError
{
    /// <summary>
    /// Gets the error code.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("code")]
    public required string Code { get; init; }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>
    /// Gets the error details.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("details")]
    public string? Details { get; init; }

    /// <summary>
    /// Gets a value indicating whether the error is retryable.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("retryable")]
    public bool Retryable { get; init; }
}
