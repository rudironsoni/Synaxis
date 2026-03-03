// <copyright file="SecurityValidationResult.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Security
{
    using System.Collections.Generic;

    /// <summary>
    /// Result of security configuration validation.
    /// </summary>
    public class SecurityValidationResult
    {
        /// <summary>
        /// Gets the list of validation errors.
        /// </summary>
        public ICollection<string> Errors { get; } = new List<string>();

        /// <summary>
        /// Gets the list of validation warnings.
        /// </summary>
        public ICollection<string> Warnings { get; } = new List<string>();

        /// <summary>
        /// Gets a value indicating whether there are any errors.
        /// </summary>
        public bool HasErrors => this.Errors.Count > 0;

        /// <summary>
        /// Gets a value indicating whether there are any warnings.
        /// </summary>
        public bool HasWarnings => this.Warnings.Count > 0;

        /// <summary>
        /// Gets a value indicating whether the validation passed (no errors).
        /// </summary>
        public bool IsValid => !this.HasErrors;

        /// <summary>
        /// Adds an error message to the validation result.
        /// </summary>
        /// <param name="error">The error message to add.</param>
        public void AddError(string error) => this.Errors.Add(error);

        /// <summary>
        /// Adds a warning message to the validation result.
        /// </summary>
        /// <param name="warning">The warning message to add.</param>
        public void AddWarning(string warning) => this.Warnings.Add(warning);
    }
}