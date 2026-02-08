// <copyright file="ErrorCodes.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.Errors
{
    /// <summary>
    /// Defines common error codes used throughout the Synaxis system.
    /// </summary>
    public static class ErrorCodes
    {
        // Authentication and Authorization errors (AUTH_*)

        /// <summary>
        /// Authentication failed due to invalid credentials.
        /// </summary>
        public const string AuthenticationFailed = "AUTH_FAILED";

        /// <summary>
        /// Authorization denied due to insufficient permissions.
        /// </summary>
        public const string AuthorizationDenied = "AUTH_DENIED";

        /// <summary>
        /// API key is invalid or expired.
        /// </summary>
        public const string InvalidApiKey = "AUTH_INVALID_API_KEY";

        // Rate limiting errors (RATE_*)

        /// <summary>
        /// Rate limit exceeded for the current user or organization.
        /// </summary>
        public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";

        /// <summary>
        /// Quota exceeded for the current billing period.
        /// </summary>
        public const string QuotaExceeded = "RATE_QUOTA_EXCEEDED";

        // Provider errors (PROVIDER_*)

        /// <summary>
        /// The specified provider is not available.
        /// </summary>
        public const string ProviderUnavailable = "PROVIDER_UNAVAILABLE";

        /// <summary>
        /// The provider returned an error response.
        /// </summary>
        public const string ProviderError = "PROVIDER_ERROR";

        /// <summary>
        /// Timeout occurred while waiting for provider response.
        /// </summary>
        public const string ProviderTimeout = "PROVIDER_TIMEOUT";

        /// <summary>
        /// The specified model is not supported by the provider.
        /// </summary>
        public const string ModelNotSupported = "PROVIDER_MODEL_NOT_SUPPORTED";

        // Validation errors (VALIDATION_*)

        /// <summary>
        /// Request validation failed.
        /// </summary>
        public const string ValidationFailed = "VALIDATION_FAILED";

        /// <summary>
        /// A required field is missing from the request.
        /// </summary>
        public const string RequiredFieldMissing = "VALIDATION_REQUIRED_FIELD_MISSING";

        /// <summary>
        /// A field value is invalid.
        /// </summary>
        public const string InvalidFieldValue = "VALIDATION_INVALID_FIELD_VALUE";

        // System errors (SYSTEM_*)

        /// <summary>
        /// An unexpected internal error occurred.
        /// </summary>
        public const string InternalError = "SYSTEM_INTERNAL_ERROR";

        /// <summary>
        /// The requested resource was not found.
        /// </summary>
        public const string NotFound = "SYSTEM_NOT_FOUND";

        /// <summary>
        /// The service is temporarily unavailable.
        /// </summary>
        public const string ServiceUnavailable = "SYSTEM_SERVICE_UNAVAILABLE";
    }
}
