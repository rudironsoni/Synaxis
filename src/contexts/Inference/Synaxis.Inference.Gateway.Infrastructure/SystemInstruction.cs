// <copyright file="SystemInstruction.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a system instruction in the Antigravity API format.
    /// </summary>
    internal class SystemInstruction
    {
        /// <summary>
        /// Gets or sets the parts of the system instruction.
        /// </summary>
        [JsonPropertyName("parts")]
        public List<Part> Parts { get; set; } = new();
    }
}