// <copyright file="AzureOpenAIOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.Azure.Configuration
{
    using System;

    /// <summary>
    /// Configuration options for Azure OpenAI Service.
    /// </summary>
    public class AzureOpenAIOptions
    {
        /// <summary>
        /// Gets or sets the Azure OpenAI endpoint.
        /// </summary>
        /// <remarks>
        /// Format: https://{resource}.openai.azure.com/.
        /// </remarks>
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the deployment ID for the Azure OpenAI model.
        /// </summary>
        public string DeploymentId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the API key for authentication.
        /// </summary>
        /// <remarks>
        /// Optional if UseAzureAd is true.
        /// </remarks>
        public string? ApiKey { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use Azure AD authentication.
        /// </summary>
        /// <remarks>
        /// When true, uses DefaultAzureCredential for token-based authentication.
        /// When false, uses API key authentication.
        /// </remarks>
        public bool UseAzureAd { get; set; } = false;

        /// <summary>
        /// Gets or sets the API version to use.
        /// </summary>
        public string ApiVersion { get; set; } = "2024-02-01";

        /// <summary>
        /// Validates the configuration options.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when configuration is invalid.</exception>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(this.Endpoint))
            {
                throw new InvalidOperationException("Azure OpenAI Endpoint is required.");
            }

            if (string.IsNullOrWhiteSpace(this.DeploymentId))
            {
                throw new InvalidOperationException("Azure OpenAI DeploymentId is required.");
            }

            if (!this.UseAzureAd && string.IsNullOrWhiteSpace(this.ApiKey))
            {
                throw new InvalidOperationException("ApiKey is required when UseAzureAd is false.");
            }
        }
    }
}
