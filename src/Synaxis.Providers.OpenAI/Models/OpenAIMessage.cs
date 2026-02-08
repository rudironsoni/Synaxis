// <copyright file="OpenAIMessage.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.OpenAI.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a message in an OpenAI chat request.
    /// </summary>
    internal sealed class OpenAIMessage
    {
        /// <summary>
        /// Gets or initializes the role.
        /// </summary>
        [JsonPropertyName("role")]
        public string Role { get; init; } = string.Empty;

        /// <summary>
        /// Gets or initializes the content.
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; init; } = string.Empty;

        /// <summary>
        /// Gets or initializes the optional name.
        /// </summary>
        [JsonPropertyName("name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Name { get; init; }
    }
}
