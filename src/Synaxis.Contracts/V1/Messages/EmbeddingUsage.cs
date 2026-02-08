// <copyright file="EmbeddingUsage.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.Messages
{
    /// <summary>
    /// Represents token usage statistics for an embedding request.
    /// </summary>
    public sealed class EmbeddingUsage
    {
        /// <summary>
        /// Gets or initializes the number of tokens in the prompt.
        /// </summary>
        public int PromptTokens { get; init; }

        /// <summary>
        /// Gets or initializes the total number of tokens used.
        /// </summary>
        public int TotalTokens { get; init; }
    }
}
