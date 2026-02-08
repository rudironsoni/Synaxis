// <copyright file="OpenAIErrorResponse.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.OpenAI.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents an OpenAI error response.
    /// </summary>
    internal sealed class OpenAIErrorResponse
    {
        /// <summary>
        /// Gets or initializes the error details.
        /// </summary>
        [JsonPropertyName("error")]
        public OpenAIError? Error { get; init; }
    }
}
