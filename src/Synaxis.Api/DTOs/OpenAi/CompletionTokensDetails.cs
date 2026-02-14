// <copyright file="CompletionTokensDetails.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.OpenAi
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents completion tokens details.
    /// </summary>
    public sealed class CompletionTokensDetails
    {
        /// <summary>
        /// Gets or sets the reasoning tokens.
        /// </summary>
        [JsonPropertyName("reasoning_tokens")]
        public int? ReasoningTokens { get; set; }
    }
}
