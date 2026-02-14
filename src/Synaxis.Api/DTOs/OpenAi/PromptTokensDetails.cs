// <copyright file="PromptTokensDetails.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.OpenAi
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents prompt tokens details.
    /// </summary>
    public sealed class PromptTokensDetails
    {
        /// <summary>
        /// Gets or sets the number of cached prompt tokens.
        /// </summary>
        [JsonPropertyName("cached_tokens")]
        public int? CachedTokens { get; set; }
    }
}
