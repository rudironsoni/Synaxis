// <copyright file="TokenUsage.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Streaming
{
    /// <summary>
    /// Represents token usage information.
    /// </summary>
    public class TokenUsage
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
        /// Gets the total number of tokens used.
        /// </summary>
        public int TotalTokens => this.PromptTokens + this.CompletionTokens;
    }
}
