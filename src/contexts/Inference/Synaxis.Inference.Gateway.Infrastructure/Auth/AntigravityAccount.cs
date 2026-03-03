// <copyright file="AntigravityAccount.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Auth
{
    using Synaxis.InferenceGateway.Infrastructure.Identity.Core;

    /// <summary>
    /// Represents an Antigravity account with token and project information.
    /// </summary>
    public class AntigravityAccount
    {
        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the project identifier.
        /// </summary>
        public string ProjectId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Token.
        /// </summary>
        public TokenResponse Token { get; set; } = new();
    }
}