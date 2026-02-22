// <copyright file="SynaxisExceptionFilter.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Transport.Http.Filters
{
    using System;
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;
    using Microsoft.Extensions.Logging;
    using Synaxis.Contracts.V1.Errors;

    /// <summary>
    /// Global exception filter that converts exceptions to proper HTTP responses with SynaxisError body.
    /// </summary>
    public class SynaxisExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<SynaxisExceptionFilter> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SynaxisExceptionFilter"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public SynaxisExceptionFilter(ILogger<SynaxisExceptionFilter> logger)
        {
            this.logger = logger!;
        }

        /// <summary>
        /// Called when an exception occurs during request processing.
        /// </summary>
        /// <param name="context">The exception context.</param>
        public void OnException(ExceptionContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            var error = this.MapExceptionToError(context.Exception);
            var statusCode = GetStatusCode(error);

            this.logger.LogError(
                context.Exception,
                "Request failed with error code {ErrorCode}: {ErrorMessage}",
                error.Code,
                error.Message);

            context.Result = new ObjectResult(error)
            {
                StatusCode = statusCode,
            };

            context.ExceptionHandled = true;
        }

        private SynaxisError MapExceptionToError(Exception exception)
        {
            // Map specific exception types to appropriate error codes
            return exception switch
            {
                ArgumentNullException or ArgumentException => new SynaxisError
                {
                    Code = ErrorCodes.ValidationFailed,
                    Message = exception.Message,
                    Severity = ErrorSeverity.Error,
                    Category = ErrorCategory.Validation,
                    Details = new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        ["ExceptionType"] = exception.GetType().Name,
                    },
                },
                UnauthorizedAccessException => new SynaxisError
                {
                    Code = ErrorCodes.AuthorizationDenied,
                    Message = "Access denied",
                    Severity = ErrorSeverity.Error,
                    Category = ErrorCategory.Auth,
                    Details = new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        ["ExceptionType"] = exception.GetType().Name,
                    },
                },
                TimeoutException => new SynaxisError
                {
                    Code = ErrorCodes.ProviderTimeout,
                    Message = "Request timeout",
                    Severity = ErrorSeverity.Error,
                    Category = ErrorCategory.Provider,
                    Details = new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        ["ExceptionType"] = exception.GetType().Name,
                    },
                },
                _ => new SynaxisError
                {
                    Code = ErrorCodes.InternalError,
                    Message = "An internal error occurred",
                    Severity = ErrorSeverity.Error,
                    Category = ErrorCategory.System,
                    Details = new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        ["ExceptionType"] = exception.GetType().Name,
                        ["Message"] = exception.Message,
                    },
                },
            };
        }

        private static int GetStatusCode(SynaxisError error)
        {
            return error.Category switch
            {
                ErrorCategory.Auth => StatusCodes.Status401Unauthorized,
                ErrorCategory.RateLimit => StatusCodes.Status429TooManyRequests,
                ErrorCategory.Validation => StatusCodes.Status400BadRequest,
                ErrorCategory.Provider => StatusCodes.Status502BadGateway,
                ErrorCategory.System when string.Equals(error.Code, ErrorCodes.NotFound, StringComparison.Ordinal) => StatusCodes.Status404NotFound,
                ErrorCategory.System when string.Equals(error.Code, ErrorCodes.ServiceUnavailable, StringComparison.Ordinal) => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status500InternalServerError,
            };
        }
    }
}
