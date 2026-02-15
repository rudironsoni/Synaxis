// <copyright file="UserDto.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.Authentication
{
    using System;

    /// <summary>
    /// Data transfer object for user information.
    /// </summary>
    public class UserDto
    {
        /// <summary>
        /// Gets or sets the unique identifier of the user.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the user's email address.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the user's first name.
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the user's last name.
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Gets the user's full name.
        /// </summary>
        public string FullName => $"{this.FirstName} {this.LastName}".Trim();

        /// <summary>
        /// Gets or sets the URL of the user's avatar image.
        /// </summary>
        public string AvatarUrl { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the email was verified.
        /// </summary>
        public DateTime? EmailVerifiedAt { get; set; }

        /// <summary>
        /// Gets a value indicating whether the user's email has been verified.
        /// </summary>
        public bool EmailVerified => this.EmailVerifiedAt.HasValue;

        /// <summary>
        /// Gets or sets the user's role.
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether multi-factor authentication is enabled for the user.
        /// </summary>
        public bool MfaEnabled { get; set; }
    }
}
