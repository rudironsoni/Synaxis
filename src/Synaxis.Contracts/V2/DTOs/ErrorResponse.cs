namespace Synaxis.Contracts.V2.DTOs;

/// <summary>
/// Standard error response format (V2).
/// </summary>
/// <remarks>
/// V2 Breaking Changes:
/// - Added RequestId for correlation
/// - Added Path for the request path
/// - ValidationErrors is now a dictionary for easier access
/// </remarks>
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
    /// Request identifier for correlation.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("requestId")]
    public string? RequestId { get; init; }

    /// <summary>
    /// Trace identifier for debugging.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("traceId")]
    public string? TraceId { get; init; }

    /// <summary>
    /// Request path that caused the error.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("path")]
    public string? Path { get; init; }

    /// <summary>
    /// Timestamp when the error occurred.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Additional error details for validation errors.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("validationErrors")]
    public Dictionary<string, IReadOnlyList<string>>? ValidationErrors { get; init; }
}
