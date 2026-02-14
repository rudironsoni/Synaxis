// <copyright file="Usage.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.DTOs.MultiModal
{
    /// <summary>
    /// Represents token usage statistics.
    /// </summary>
    public class Usage
    {
        /// <summary>
        /// Gets or sets the number of prompt tokens used.
        /// </summary>
        public int PromptTokens { get; set; }

        /// <summary>
        /// Gets or sets the number of completion tokens used.
        /// </summary>
        public int CompletionTokens { get; set; }

        /// <summary>
        /// Gets or sets the total number of tokens used.
        /// </summary>
        public int TotalTokens { get; set; }
    }
}
