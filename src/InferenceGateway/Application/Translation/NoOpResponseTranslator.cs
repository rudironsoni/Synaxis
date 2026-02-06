// <copyright file="NoOpResponseTranslator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Translation
{
    /// <summary>
    /// No-operation implementation of response translator that handles no responses.
    /// </summary>
    public sealed class NoOpResponseTranslator : IResponseTranslator
    {
        /// <summary>
        /// Determines whether this translator can handle the specified response.
        /// </summary>
        /// <param name="response">The canonical response to check.</param>
        /// <returns>Always returns false.</returns>
        public bool CanHandle(CanonicalResponse response)
        {
            return false;
        }

        /// <summary>
        /// Translates the canonical response without modification.
        /// </summary>
        /// <param name="response">The canonical response to translate.</param>
        /// <returns>The unmodified canonical response.</returns>
        public CanonicalResponse Translate(CanonicalResponse response)
        {
            return response;
        }
    }
}
