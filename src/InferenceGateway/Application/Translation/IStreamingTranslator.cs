// <copyright file="IStreamingTranslator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Translation
{
    using Microsoft.Extensions.AI;

    /// <summary>
    /// Defines the contract for translating streaming chat response updates.
    /// </summary>
    public interface IStreamingTranslator
    {
        /// <summary>
        /// Determines whether this translator can handle the specified update.
        /// </summary>
        /// <param name="update">The chat response update to check.</param>
        /// <returns>True if this translator can handle the update; otherwise, false.</returns>
        bool CanHandle(ChatResponseUpdate update);

        /// <summary>
        /// Translates the chat response update.
        /// </summary>
        /// <param name="update">The chat response update to translate.</param>
        /// <returns>The translated chat response update.</returns>
        ChatResponseUpdate Translate(ChatResponseUpdate update);
    }
}
