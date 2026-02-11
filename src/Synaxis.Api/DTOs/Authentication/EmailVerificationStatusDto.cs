// <copyright file="EmailVerificationStatusDto.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.Authentication
{
    using System;

    /// <summary>
    /// DTO for email verification status.
    /// </summary>
    public class EmailVerificationStatusDto
    {
        /// <summary>
        /// Gets or sets a value indicating whether the email is verified.
        /// </summary>
        public bool IsVerified { get; set; }

        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the verification timestamp.
        /// </summary>
        public DateTime? VerifiedAt { get; set; }
    }
}
