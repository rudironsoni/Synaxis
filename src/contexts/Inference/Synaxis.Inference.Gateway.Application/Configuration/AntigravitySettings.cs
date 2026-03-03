// <copyright file="AntigravitySettings.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Configuration
{
    using System;

    /// <summary>
    /// Settings for Antigravity integration.
    /// </summary>
    public class AntigravitySettings
    {
        /// <summary>
        /// Gets the default redirect host for local auth flows.
        /// </summary>
        public const string DefaultRedirectHost = "localhost";

        /// <summary>
        /// Gets the default redirect path for local auth flows.
        /// </summary>
        public const string DefaultRedirectPath = "/oauth/antigravity/callback";

        /// <summary>
        /// Gets the default redirect port for local auth flows.
        /// </summary>
        public const int DefaultRedirectPort = 51121;

        /// <summary>
        /// Gets the default redirect URL for local auth flows.
        /// </summary>
        public static string DefaultRedirectUrl => new UriBuilder(
            Uri.UriSchemeHttp,
            DefaultRedirectHost,
            DefaultRedirectPort,
            DefaultRedirectPath).Uri.ToString();

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
