// <copyright file="ChatUsage.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Contracts.V1.Messages
{
    /// <summary>
    /// Represents token usage statistics for a chat completion.
    /// </summary>
    public sealed class ChatUsage
    {
        /// <summary>
        /// Gets or initializes the number of tokens in the prompt.
        /// </summary>
        public int PromptTokens { get; init; }

        /// <summary>
        /// Gets or initializes the number of tokens in the completion.
        /// </summary>
        public int CompletionTokens { get; init; }

        /// <summary>
        /// Gets or initializes the total number of tokens used.
        /// </summary>
        public int TotalTokens { get; init; }
    }
}
