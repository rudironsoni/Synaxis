// <copyright file="OpenAIOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.Configuration
{
    /// <summary>
    /// Configuration options for the OpenAI provider adapter.
    /// </summary>
    public sealed class OpenAIOptions
    {
        /// <summary>
        /// Gets or sets the OpenAI API key.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional organization ID.
        /// </summary>
        public string? OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the base URL for the OpenAI API.
        /// </summary>
        public string BaseUrl { get; set; } = "https://api.openai.com/v1/";

        /// <summary>
        /// Gets or sets the maximum number of retry attempts.
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Gets or sets the timeout for HTTP requests in seconds.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 120;
    }
}
