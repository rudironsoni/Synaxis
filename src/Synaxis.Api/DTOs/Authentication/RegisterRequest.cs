// <copyright file="RegisterRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.Authentication
{
    using System;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Request model for user registration.
    /// </summary>
    public class RegisterRequest
    {
        /// <summary>
        /// Gets or sets the organization ID to associate the user with.
        /// </summary>
        [Required]
        public Guid? OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the user's email address.
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the user's password.
        /// </summary>
        [Required]
        [MinLength(8)]
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the user's first name.
        /// </summary>
        [Required]
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the user's last name.
        /// </summary>
        [Required]
        public string LastName { get; set; }

        /// <summary>
        /// Gets or sets the data residency region for the user's data.
        /// </summary>
        public string DataResidencyRegion { get; set; }

        /// <summary>
        /// Gets or sets the region where the user account was created.
        /// </summary>
        public string CreatedInRegion { get; set; }
    }
}
