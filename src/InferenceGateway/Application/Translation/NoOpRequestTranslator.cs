// <copyright file="NoOpRequestTranslator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Translation
{
    /// <summary>
    /// No-operation implementation of request translator that handles no requests.
    /// </summary>
    public sealed class NoOpRequestTranslator : IRequestTranslator
    {
        /// <summary>
        /// Determines whether this translator can handle the specified request.
        /// </summary>
        /// <param name="request">The canonical request to check.</param>
        /// <returns>Always returns false.</returns>
        public bool CanHandle(CanonicalRequest request)
        {
            return false;
        }

        /// <summary>
        /// Translates the canonical request without modification.
        /// </summary>
        /// <param name="request">The canonical request to translate.</param>
        /// <returns>The unmodified canonical request.</returns>
        public CanonicalRequest Translate(CanonicalRequest request)
        {
            return request;
        }
    }
}
