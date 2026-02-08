// <copyright file="AnthropicOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.Anthropic.Configuration
{
    using System;

    /// <summary>
    /// Configuration options for the Anthropic provider.
    /// </summary>
    public sealed class AnthropicOptions
    {
        /// <summary>
        /// Gets or sets the Anthropic API key.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the base URL for the Anthropic API.
        /// </summary>
        public string BaseUrl { get; set; } = "https://api.anthropic.com/v1/";

        /// <summary>
        /// Gets or sets the Anthropic API version header value (e.g., "2023-06-01").
        /// </summary>
        public string? AnthropicVersion { get; set; }

        /// <summary>
        /// Validates the options.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when required options are missing or invalid.</exception>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(this.ApiKey))
            {
                throw new InvalidOperationException("AnthropicOptions.ApiKey cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(this.BaseUrl))
            {
                throw new InvalidOperationException("AnthropicOptions.BaseUrl cannot be null or empty.");
            }

            if (!Uri.TryCreate(this.BaseUrl, UriKind.Absolute, out _))
            {
                throw new InvalidOperationException("AnthropicOptions.BaseUrl must be a valid absolute URI.");
            }
        }
    }
}
