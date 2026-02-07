// <copyright file="RegistrationResult.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Identity.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Registration result.
    /// </summary>
    public class RegistrationResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the operation was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the created user ID.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the created organization ID (if applicable).
        /// </summary>
        public Guid? OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the created user information.
        /// </summary>
        public UserInfo? User { get; set; }

        /// <summary>
        /// Gets or sets the created organization information.
        /// </summary>
        public OrganizationInfo? Organization { get; set; }

        /// <summary>
        /// Gets or sets the error messages if operation failed.
        /// </summary>
        public IList<string> Errors { get; set; } = new List<string>();
    }
}
