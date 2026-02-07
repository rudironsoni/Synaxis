// <copyright file="SynaxisConfiguration.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Configuration
{
    using System.Collections.Generic;

    /// <summary>
    /// Main configuration for Synaxis Inference Gateway.
    /// </summary>
    public class SynaxisConfiguration
    {
        /// <summary>
        /// Gets or sets the providers configuration.
        /// </summary>
        public IDictionary<string, ProviderConfig> Providers { get; set; } = new Dictionary<string, ProviderConfig>(StringComparer.Ordinal);

        /// <summary>
        /// Gets or sets the canonical models configuration.
        /// </summary>
        public IList<CanonicalModelConfig> CanonicalModels { get; set; } = new List<CanonicalModelConfig>();

        /// <summary>
        /// Gets or sets the aliases configuration.
        /// </summary>
        public IDictionary<string, AliasConfig> Aliases { get; set; } = new Dictionary<string, AliasConfig>(StringComparer.Ordinal);

        /// <summary>
        /// Gets or sets the master key.
        /// </summary>
        public string? MasterKey { get; set; }

        /// <summary>
        /// Gets or sets the JWT secret.
        /// </summary>
        public string? JwtSecret { get; set; }

        /// <summary>
        /// Gets or sets the JWT issuer.
        /// </summary>
        public string? JwtIssuer { get; set; }

        /// <summary>
        /// Gets or sets the JWT audience.
        /// </summary>
        public string? JwtAudience { get; set; }

        /// <summary>
        /// Gets or sets the Antigravity settings.
        /// </summary>
        public AntigravitySettings? Antigravity { get; set; }

        /// <summary>
        /// Gets or sets the maximum allowed request body size (bytes) for parsing incoming OpenAI-compatible requests.
        /// Default set to 30 MB (31457280 bytes).
        /// </summary>
        public long MaxRequestBodySize { get; set; } = 31457280;
    }
}
