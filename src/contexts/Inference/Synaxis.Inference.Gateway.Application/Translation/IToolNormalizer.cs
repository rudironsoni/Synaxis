// <copyright file="IToolNormalizer.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Translation
{
    using Microsoft.Extensions.AI;

    /// <summary>
    /// Normalizes tool calls in responses and updates.
    /// </summary>
    public interface IToolNormalizer
    {
        /// <summary>
        /// Normalizes tool calls in a canonical response.
        /// </summary>
        /// <param name="response">The response to normalize.</param>
        /// <returns>The normalized response.</returns>
        CanonicalResponse NormalizeResponse(CanonicalResponse response);

        /// <summary>
        /// Normalizes tool calls in a chat response update.
        /// </summary>
        /// <param name="update">The update to normalize.</param>
        /// <returns>The normalized update.</returns>
        ChatResponseUpdate NormalizeUpdate(ChatResponseUpdate update);
    }
}
