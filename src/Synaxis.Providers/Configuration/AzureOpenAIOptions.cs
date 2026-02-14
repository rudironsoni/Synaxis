// <copyright file="AzureOpenAIOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.Configuration
{
    /// <summary>
    /// Configuration options for the Azure OpenAI provider adapter.
    /// </summary>
    public sealed class AzureOpenAIOptions
    {
        /// <summary>
        /// Gets or sets the Azure OpenAI endpoint URL.
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Azure OpenAI API key (optional if using Azure AD).
        /// </summary>
        public string? ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the deployment name for chat completions.
        /// </summary>
        public string ChatDeploymentName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the deployment name for embeddings.
        /// </summary>
        public string EmbeddingDeploymentName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether to use Azure AD authentication.
        /// </summary>
        public bool UseAzureAd { get; set; }

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
