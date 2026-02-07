// <copyright file="ITranslationPipeline.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Translation
{
    using Microsoft.Extensions.AI;

    /// <summary>
    /// Defines the contract for a translation pipeline that processes requests, responses, and streaming updates.
    /// </summary>
    public interface ITranslationPipeline
    {
        /// <summary>
        /// Translates a canonical request.
        /// </summary>
        /// <param name="request">The canonical request to translate.</param>
        /// <returns>The translated canonical request.</returns>
        CanonicalRequest TranslateRequest(CanonicalRequest request);

        /// <summary>
        /// Translates a canonical response.
        /// </summary>
        /// <param name="response">The canonical response to translate.</param>
        /// <returns>The translated canonical response.</returns>
        CanonicalResponse TranslateResponse(CanonicalResponse response);

        /// <summary>
        /// Translates a chat response update for streaming scenarios.
        /// </summary>
        /// <param name="update">The chat response update to translate.</param>
        /// <returns>The translated chat response update.</returns>
        ChatResponseUpdate TranslateUpdate(ChatResponseUpdate update);
    }
}
