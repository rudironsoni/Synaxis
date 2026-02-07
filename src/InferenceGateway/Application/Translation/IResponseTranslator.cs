// <copyright file="IResponseTranslator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Translation
{
    /// <summary>
    /// Defines the contract for translating canonical responses.
    /// </summary>
    public interface IResponseTranslator
    {
        /// <summary>
        /// Determines whether this translator can handle the specified response.
        /// </summary>
        /// <param name="response">The canonical response to check.</param>
        /// <returns>True if this translator can handle the response; otherwise, false.</returns>
        bool CanHandle(CanonicalResponse response);

        /// <summary>
        /// Translates the canonical response.
        /// </summary>
        /// <param name="response">The canonical response to translate.</param>
        /// <returns>The translated canonical response.</returns>
        CanonicalResponse Translate(CanonicalResponse response);
    }
}
