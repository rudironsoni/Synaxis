// <copyright file="CanonicalModelConfig.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Configuration
{
    /// <summary>
    /// Configuration for a canonical model.
    /// </summary>
    public class CanonicalModelConfig
    {
        /// <summary>
        /// Gets or sets the model ID.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the provider name.
        /// </summary>
        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model path.
        /// </summary>
        public string ModelPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether streaming is supported.
        /// </summary>
        public bool Streaming { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether tools are supported.
        /// </summary>
        public bool Tools { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether vision is supported.
        /// </summary>
        public bool Vision { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether structured output is supported.
        /// </summary>
        public bool StructuredOutput { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether log probs are supported.
        /// </summary>
        public bool LogProbs { get; set; }
    }
}
