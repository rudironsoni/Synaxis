// <copyright file="ErrorCodes.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Middleware
{
    using System.Net;
    /// <summary>
    /// Canonical error codes for consistent error handling across the Synaxis API.
    /// Based on OpenAI error format with additional Synaxis-specific codes.
    /// </summary>
    public static class ErrorCodes
    {
    /// <summary>
    /// Client-side validation errors (400 Bad Request)
    /// </summary>
    public const string InvalidRequestError = "invalid_request_error";

    /// <summary>
    /// Invalid value provided for a parameter.
    /// </summary>
    public const string InvalidValue = "invalid_value";

    /// <summary>
    /// Required field is missing from the request.
    /// </summary>
    public const string MissingRequiredField = "missing_required_field";

    /// <summary>
    /// Parameter has an invalid type.
    /// </summary>
    public const string InvalidParameterType = "invalid_parameter_type";

    /// <summary>
    /// Request body contains invalid JSON.
    /// </summary>
    public const string InvalidJson = "invalid_json";

    /// <summary>
    /// Authentication and authorization errors (401, 403)
    /// </summary>
    public const string AuthenticationError = "authentication_error";

    /// <summary>
    /// The API key provided is invalid.
    /// </summary>
    public const string InvalidApiKey = "invalid_api_key";

    /// <summary>
    /// The API key provided has expired.
    /// </summary>
    public const string ExpiredApiKey = "expired_api_key";

    /// <summary>
    /// Access to the resource is forbidden.
    /// </summary>
    public const string Forbidden = "forbidden";

    /// <summary>
    /// The user has insufficient permissions for the requested operation.
    /// </summary>
    public const string InsufficientPermissions = "insufficient_permissions";

    /// <summary>
    /// Resource not found errors (404)
    /// </summary>
    public const string NotFound = "not_found";

    /// <summary>
    /// The requested model was not found.
    /// </summary>
    public const string ModelNotFound = "model_not_found";

    /// <summary>
    /// The requested provider was not found.
    /// </summary>
    public const string ProviderNotFound = "provider_not_found";

    /// <summary>
    /// Rate limiting errors (429)
    /// </summary>
    public const string RateLimitExceeded = "rate_limit_exceeded";

    /// <summary>
    /// Usage quota has been exceeded.
    /// </summary>
    public const string QuotaExceeded = "quota_exceeded";

    /// <summary>
    /// Upstream provider errors (502, 503, 504)
    /// </summary>
    public const string UpstreamRoutingFailure = "upstream_routing_failure";

    /// <summary>
    /// The upstream provider returned an error.
    /// </summary>
    public const string ProviderError = "provider_error";

    /// <summary>
    /// Bad gateway error from upstream provider.
    /// </summary>
    public const string BadGateway = "bad_gateway";

    /// <summary>
    /// The service is temporarily unavailable.
    /// </summary>
    public const string ServiceUnavailable = "service_unavailable";

    /// <summary>
    /// The gateway timed out waiting for a response.
    /// </summary>
    public const string GatewayTimeout = "gateway_timeout";

    /// <summary>
    /// Internal server errors (500)
    /// </summary>
    public const string InternalError = "internal_error";

    /// <summary>
    /// An unexpected server error occurred.
    /// </summary>
    public const string ServerError = "server_error";
    }

    /// <summary>
    /// Maps error codes to HTTP status codes for consistent error responses.
    /// </summary>
    public static class ErrorCodeMappings
    {
    /// <summary>
    /// Gets the HTTP status code for a given error code.
    /// </summary>
    /// <param name="errorCode">The error code to map.</param>
    /// <returns>The corresponding HTTP status code.</returns>
    public static HttpStatusCode GetStatusCode(string errorCode)
    {
        return errorCode switch
        {
            // Client validation errors (400)
            ErrorCodes.InvalidRequestError => HttpStatusCode.BadRequest,
            ErrorCodes.InvalidValue => HttpStatusCode.BadRequest,
            ErrorCodes.MissingRequiredField => HttpStatusCode.BadRequest,
            ErrorCodes.InvalidParameterType => HttpStatusCode.BadRequest,
            ErrorCodes.InvalidJson => HttpStatusCode.BadRequest,

            // Authentication errors (401)
            ErrorCodes.AuthenticationError => HttpStatusCode.Unauthorized,
            ErrorCodes.InvalidApiKey => HttpStatusCode.Unauthorized,
            ErrorCodes.ExpiredApiKey => HttpStatusCode.Unauthorized,

            // Authorization errors (403)
            ErrorCodes.Forbidden => HttpStatusCode.Forbidden,
            ErrorCodes.InsufficientPermissions => HttpStatusCode.Forbidden,

            // Not found errors (404)
            ErrorCodes.NotFound => HttpStatusCode.NotFound,
            ErrorCodes.ModelNotFound => HttpStatusCode.NotFound,
            ErrorCodes.ProviderNotFound => HttpStatusCode.NotFound,

            // Rate limiting (429)
            ErrorCodes.RateLimitExceeded => HttpStatusCode.TooManyRequests,
            ErrorCodes.QuotaExceeded => HttpStatusCode.TooManyRequests,

            // Upstream provider errors (502, 503, 504)
            ErrorCodes.UpstreamRoutingFailure => HttpStatusCode.BadGateway,
            ErrorCodes.ProviderError => HttpStatusCode.BadGateway,
            ErrorCodes.BadGateway => HttpStatusCode.BadGateway,
            ErrorCodes.ServiceUnavailable => HttpStatusCode.ServiceUnavailable,
            ErrorCodes.GatewayTimeout => HttpStatusCode.GatewayTimeout,

            // Internal errors (500)
            ErrorCodes.InternalError => HttpStatusCode.InternalServerError,
            ErrorCodes.ServerError => HttpStatusCode.InternalServerError,

            // Default to 500 for unknown error codes
            _ => HttpStatusCode.InternalServerError
        };
    }

    /// <summary>
    /// Gets the OpenAI-compatible error type for a given error code.
    /// </summary>
    /// <param name="errorCode">The error code to map.</param>
    /// <returns>The corresponding error type.</returns>
    public static string GetErrorType(string errorCode)
    {
        return errorCode switch
        {
            // Client validation errors
            ErrorCodes.InvalidRequestError => "invalid_request_error",
            ErrorCodes.InvalidValue => "invalid_request_error",
            ErrorCodes.MissingRequiredField => "invalid_request_error",
            ErrorCodes.InvalidParameterType => "invalid_request_error",
            ErrorCodes.InvalidJson => "invalid_request_error",

            // Authentication errors
            ErrorCodes.AuthenticationError => "authentication_error",
            ErrorCodes.InvalidApiKey => "authentication_error",
            ErrorCodes.ExpiredApiKey => "authentication_error",

            // Authorization errors
            ErrorCodes.Forbidden => "permission_error",
            ErrorCodes.InsufficientPermissions => "permission_error",

            // Not found errors
            ErrorCodes.NotFound => "invalid_request_error",
            ErrorCodes.ModelNotFound => "invalid_request_error",
            ErrorCodes.ProviderNotFound => "invalid_request_error",

            // Rate limiting
            ErrorCodes.RateLimitExceeded => "rate_limit_error",
            ErrorCodes.QuotaExceeded => "rate_limit_error",

            // Upstream provider errors
            ErrorCodes.UpstreamRoutingFailure => "api_error",
            ErrorCodes.ProviderError => "api_error",
            ErrorCodes.BadGateway => "api_error",
            ErrorCodes.ServiceUnavailable => "api_error",
            ErrorCodes.GatewayTimeout => "api_error",

            // Internal errors
            ErrorCodes.InternalError => "server_error",
            ErrorCodes.ServerError => "server_error",

            // Default
            _ => "api_error"
        };
    }
    }

}