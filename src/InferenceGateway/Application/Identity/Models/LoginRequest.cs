// <copyright file="LoginRequest.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Identity.Models
{
    using System;

    /// <summary>
    /// Request model for user login.
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// Gets or sets the user's email address.
        /// </summary>
        required public string Email { get; set; }

        /// <summary>
        /// Gets or sets the user's password.
        /// </summary>
        required public string Password { get; set; }

        /// <summary>
        /// Gets or sets the optional organization ID to log into.
        /// </summary>
        public Guid? OrganizationId { get; set; }
    }
}
