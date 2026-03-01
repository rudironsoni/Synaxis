// <copyright file="ErrorResponse.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.DTOs;

/// <summary>
/// Standard error response format.
/// </summary>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(ErrorResponse), "error")]
public record ErrorResponse
{
    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("statusCode")]
    public required int StatusCode { get; init; }

    /// <summary>
    /// Gets the error code for programmatic handling.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("code")]
    public required string Code { get; init; }

    /// <summary>
    /// Gets the human-readable error message.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>
    /// Gets the detailed error description.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("details")]
    public string? Details { get; init; }

    /// <summary>
    /// Gets the trace identifier for debugging.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("traceId")]
    public string? TraceId { get; init; }

    /// <summary>
    /// Gets the timestamp when the error occurred.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the additional error details for validation errors.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("validationErrors")]
    public IReadOnlyList<ValidationError>? ValidationErrors { get; init; }
}
