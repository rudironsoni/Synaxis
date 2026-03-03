// <copyright file="AuthResult.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Identity.Core
{
    /// <summary>
    /// Authentication result.
    /// </summary>
    public class AuthResult
    {
        /// <summary>
        /// Gets or sets the status (e.g., Pending, Completed, Error).
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user code.
        /// </summary>
        public string? UserCode { get; set; }

        /// <summary>
        /// Gets or sets the verification URI.
        /// </summary>
        public string? VerificationUri { get; set; }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets the token response.
        /// </summary>
        public TokenResponse? TokenResponse { get; set; }
    }
}