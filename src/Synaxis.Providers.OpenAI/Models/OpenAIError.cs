// <copyright file="OpenAIError.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.OpenAI.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents error details in an OpenAI error response.
    /// </summary>
    internal sealed class OpenAIError
    {
        /// <summary>
        /// Gets or initializes the error message.
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; init; } = string.Empty;

        /// <summary>
        /// Gets or initializes the error type.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; init; } = string.Empty;

        /// <summary>
        /// Gets or initializes the error code.
        /// </summary>
        [JsonPropertyName("code")]
        public string? Code { get; init; }
    }
}
