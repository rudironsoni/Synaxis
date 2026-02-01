namespace Synaxis.InferenceGateway.WebApi.Errors;

/// <summary>
/// Error code catalog for consistent error handling across the API.
/// Provides canonical error codes with associated HTTP status codes and user-friendly messages.
/// </summary>
public static class ErrorCodes
{
    /// <summary>
    /// Invalid request error - client provided invalid input (400)
    /// </summary>
    public const string InvalidRequestError = "invalid_request_error";

    /// <summary>
    /// Invalid value - specific parameter has invalid value (400)
    /// </summary>
    public const string InvalidValue = "invalid_value";

    /// <summary>
    /// Upstream routing failure - all providers failed (502)
    /// </summary>
    public const string UpstreamRoutingFailure = "upstream_routing_failure";

    /// <summary>
    /// Provider error - specific provider returned an error (502)
    /// </summary>
    public const string ProviderError = "provider_error";

    /// <summary>
    /// Rate limit exceeded - too many requests (429)
    /// </summary>
    public const string RateLimitExceeded = "rate_limit_exceeded";

    /// <summary>
    /// Authentication error - invalid or missing credentials (401)
    /// </summary>
    public const string AuthenticationError = "authentication_error";

    /// <summary>
    /// Authorization error - insufficient permissions (403)
    /// </summary>
    public const string AuthorizationError = "authorization_error";

    /// <summary>
    /// Not found - resource not found (404)
    /// </summary>
    public const string NotFound = "not_found";

    /// <summary>
    /// Service unavailable - service temporarily unavailable (503)
    /// </summary>
    public const string ServiceUnavailable = "service_unavailable";

    /// <summary>
    /// Internal server error - unexpected server error (500)
    /// </summary>
    public const string InternalError = "internal_error";

    /// <summary>
    /// Gets the HTTP status code for a given error code.
    /// </summary>
    /// <param name="errorCode">The error code.</param>
    /// <returns>The HTTP status code.</returns>
    public static int GetStatusCode(string errorCode)
    {
        return errorCode switch
        {
            InvalidRequestError => 400,
            InvalidValue => 400,
            AuthenticationError => 401,
            AuthorizationError => 403,
            NotFound => 404,
            RateLimitExceeded => 429,
            UpstreamRoutingFailure => 502,
            ProviderError => 502,
            ServiceUnavailable => 503,
            InternalError => 500,
            _ => 500
        };
    }

    /// <summary>
    /// Gets the error type (OpenAI-compatible) for a given error code.
    /// </summary>
    /// <param name="errorCode">The error code.</param>
    /// <returns>The error type.</returns>
    public static string GetErrorType(string errorCode)
    {
        return errorCode switch
        {
            InvalidRequestError => "invalid_request_error",
            InvalidValue => "invalid_request_error",
            AuthenticationError => "authentication_error",
            AuthorizationError => "permission_error",
            NotFound => "not_found_error",
            RateLimitExceeded => "rate_limit_error",
            UpstreamRoutingFailure => "api_error",
            ProviderError => "api_error",
            ServiceUnavailable => "api_error",
            InternalError => "server_error",
            _ => "api_error"
        };
    }

    /// <summary>
    /// Gets a user-friendly message for a given error code.
    /// </summary>
    /// <param name="errorCode">The error code.</param>
    /// <returns>A user-friendly message.</returns>
    public static string GetUserMessage(string errorCode)
    {
        return errorCode switch
        {
            InvalidRequestError => "The request was invalid or cannot be served.",
            InvalidValue => "A parameter value is invalid.",
            AuthenticationError => "Authentication failed. Please check your credentials.",
            AuthorizationError => "You do not have permission to access this resource.",
            NotFound => "The requested resource was not found.",
            RateLimitExceeded => "Rate limit exceeded. Please try again later.",
            UpstreamRoutingFailure => "Unable to route request to any provider. Please try again later.",
            ProviderError => "An error occurred while processing your request.",
            ServiceUnavailable => "The service is temporarily unavailable. Please try again later.",
            InternalError => "An internal server error occurred. Please try again later.",
            _ => "An unexpected error occurred."
        };
    }
}
