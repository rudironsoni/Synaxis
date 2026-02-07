// <copyright file="AntigravitySettings.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Configuration
{
    /// <summary>
    /// Settings for Antigravity integration.
    /// </summary>
    public class AntigravitySettings
    {
        /// <summary>
        /// Gets or sets the client ID.
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the client secret.
        /// </summary>
        public string ClientSecret { get; set; } = string.Empty;
    }
}
