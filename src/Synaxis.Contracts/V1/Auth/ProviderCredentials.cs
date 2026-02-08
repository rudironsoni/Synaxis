// <copyright file="ProviderCredentials.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.Auth
{
    /// <summary>
    /// Represents provider-specific credentials with provider name, credential type, and value.
    /// </summary>
    public sealed class ProviderCredentials
    {
        /// <summary>
        /// Gets or initializes the name of the provider (e.g., "OpenAI", "Anthropic").
        /// </summary>
        public string ProviderName { get; init; } = string.Empty;

        /// <summary>
        /// Gets or initializes the type of credential (e.g., "ApiKey", "OAuth", "ServiceAccount").
        /// </summary>
        public string CredentialType { get; init; } = string.Empty;

        /// <summary>
        /// Gets or initializes the credential value (e.g., API key, token).
        /// </summary>
        public string CredentialValue { get; init; } = string.Empty;
    }
}
