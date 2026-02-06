// <copyright file="NoOpStreamingTranslator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Translation
{
    using Microsoft.Extensions.AI;

    /// <summary>
    /// No-operation implementation of streaming translator that handles no updates.
    /// </summary>
    public sealed class NoOpStreamingTranslator : IStreamingTranslator
    {
        /// <summary>
        /// Determines whether this translator can handle the specified update.
        /// </summary>
        /// <param name="update">The chat response update to check.</param>
        /// <returns>Always returns false.</returns>
        public bool CanHandle(ChatResponseUpdate update)
        {
            return false;
        }

        /// <summary>
        /// Translates the chat response update without modification.
        /// </summary>
        /// <param name="update">The chat response update to translate.</param>
        /// <returns>The unmodified chat response update.</returns>
        public ChatResponseUpdate Translate(ChatResponseUpdate update)
        {
            return update;
        }
    }
}
