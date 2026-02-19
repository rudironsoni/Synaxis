namespace Synaxis.Contracts.V1.DTOs;

/// <summary>
/// Standard error response format.
/// </summary>
[System.Text.Json.Serialization.JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[System.Text.Json.Serialization.JsonDerivedType(typeof(ErrorResponse), "error")]
public record ErrorResponse
{
    /// <summary>
    /// HTTP status code.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("statusCode")]
    public required int StatusCode { get; init; }

    /// <summary>
    /// Error code for programmatic handling.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("code")]
    public required string Code { get; init; }

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>
    /// Detailed error description.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("details")]
    public string? Details { get; init; }

    /// <summary>
    /// Trace identifier for debugging.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("traceId")]
    public string? TraceId { get; init; }

    /// <summary>
    /// Timestamp when the error occurred.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Additional error details for validation errors.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("validationErrors")]
    public IReadOnlyList<ValidationError>? ValidationErrors { get; init; }
}

/// <summary>
/// Represents a single validation error.
/// </summary>
public record ValidationError
{
    /// <summary>
    /// Property that failed validation.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("property")]
    public required string Property { get; init; }

    /// <summary>
    /// Error message.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>
    /// Error code.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("code")]
    public string? Code { get; init; }
}
