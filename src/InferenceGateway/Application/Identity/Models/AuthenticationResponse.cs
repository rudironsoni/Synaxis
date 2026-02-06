// <copyright file="AuthenticationResponse.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Identity.Models
{
    using System;

    /// <summary>
    /// Response model for authentication operations.
    /// </summary>
    public class AuthenticationResponse
    {
        /// <summary>
        /// Gets or sets a value indicating whether the operation was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the JWT access token.
        /// </summary>
        public string? AccessToken { get; set; }

        /// <summary>
        /// Gets or sets the refresh token.
        /// </summary>
        public string? RefreshToken { get; set; }

        /// <summary>
        /// Gets or sets the token expiration time.
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the user information.
        /// </summary>
        public UserInfo? User { get; set; }

        /// <summary>
        /// Gets or sets the error message if operation failed.
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
