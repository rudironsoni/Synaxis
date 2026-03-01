// <copyright file="ExecutionContext.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V2.DTOs;

/// <summary>
/// Execution context for distributed tracing.
/// </summary>
public record ExecutionContext
{
    /// <summary>
    /// Gets the trace identifier for distributed tracing.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("traceId")]
    public string? TraceId { get; init; }

    /// <summary>
    /// Gets the span identifier for distributed tracing.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("spanId")]
    public string? SpanId { get; init; }
}
