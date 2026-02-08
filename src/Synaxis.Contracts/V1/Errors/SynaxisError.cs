// <copyright file="SynaxisError.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.Errors
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents an error with code, message, severity, category, and additional details.
    /// </summary>
    public sealed class SynaxisError
    {
        /// <summary>
        /// Gets or initializes the error code.
        /// </summary>
        public string Code { get; init; } = string.Empty;

        /// <summary>
        /// Gets or initializes the human-readable error message.
        /// </summary>
        public string Message { get; init; } = string.Empty;

        /// <summary>
        /// Gets or initializes the severity level of the error.
        /// </summary>
        public ErrorSeverity Severity { get; init; } = ErrorSeverity.Error;

        /// <summary>
        /// Gets or initializes the category of the error.
        /// </summary>
        public ErrorCategory Category { get; init; } = ErrorCategory.System;

        /// <summary>
        /// Gets or initializes additional details about the error.
        /// </summary>
        public IReadOnlyDictionary<string, object> Details { get; init; } = new Dictionary<string, object>(System.StringComparer.Ordinal);
    }

    /// <summary>
    /// Defines the severity levels for errors.
    /// </summary>
    public enum ErrorSeverity
    {
        /// <summary>
        /// Fatal error that prevents the system from functioning.
        /// </summary>
        Fatal = 0,

        /// <summary>
        /// Error that prevents the operation from completing.
        /// </summary>
        Error = 1,

        /// <summary>
        /// Warning that indicates a potential issue.
        /// </summary>
        Warning = 2,

        /// <summary>
        /// Informational message.
        /// </summary>
        Info = 3,
    }

    /// <summary>
    /// Defines the categories for errors.
    /// </summary>
    public enum ErrorCategory
    {
        /// <summary>
        /// Authentication or authorization error.
        /// </summary>
        Auth = 0,

        /// <summary>
        /// Rate limiting error.
        /// </summary>
        RateLimit = 1,

        /// <summary>
        /// Provider-specific error.
        /// </summary>
        Provider = 2,

        /// <summary>
        /// Validation error.
        /// </summary>
        Validation = 3,

        /// <summary>
        /// System or infrastructure error.
        /// </summary>
        System = 4,
    }
}
