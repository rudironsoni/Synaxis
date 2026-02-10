// <copyright file="MfaLoginRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.Authentication
{
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Request for MFA login.
    /// </summary>
    public class MfaLoginRequest
    {
        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        [Required]
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the MFA code.
        /// </summary>
        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string Code { get; set; }
    }
}
