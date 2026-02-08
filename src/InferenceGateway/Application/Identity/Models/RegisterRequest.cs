// <copyright file="RegisterRequest.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Identity.Models
{
    using System;

    /// <summary>
    /// Request model for user registration.
    /// </summary>
    public class RegisterRequest
    {
        /// <summary>
        /// Gets or sets the user's email address.
        /// </summary>
        public required string Email { get; set; }

        /// <summary>
        /// Gets or sets the user's password.
        /// </summary>
        public required string Password { get; set; }

        /// <summary>
        /// Gets or sets the user's first name.
        /// </summary>
        public string? FirstName { get; set; }

        /// <summary>
        /// Gets or sets the user's last name.
        /// </summary>
        public string? LastName { get; set; }

        /// <summary>
        /// Gets or sets the organization name (for new org registration).
        /// </summary>
        public string? OrganizationName { get; set; }

        /// <summary>
        /// Gets or sets the organization slug (for new org registration).
        /// </summary>
        public string? OrganizationSlug { get; set; }
    }
}
