// <copyright file="ValidationError.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.DTOs;

/// <summary>
/// Represents a single validation error.
/// </summary>
public record ValidationError
{
    /// <summary>
    /// Gets the property that failed validation.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("property")]
    public required string Property { get; init; }

    /// <summary>
    /// Gets the error message.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>
    /// Gets the error code.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("code")]
    public string? Code { get; init; }
}
