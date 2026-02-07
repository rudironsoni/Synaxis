// <copyright file="IRequestTranslator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Translation
{
    /// <summary>
    /// Defines the contract for translating canonical requests.
    /// </summary>
    public interface IRequestTranslator
    {
        /// <summary>
        /// Determines whether this translator can handle the specified request.
        /// </summary>
        /// <param name="request">The canonical request to check.</param>
        /// <returns>True if this translator can handle the request; otherwise, false.</returns>
        bool CanHandle(CanonicalRequest request);

        /// <summary>
        /// Translates the canonical request.
        /// </summary>
        /// <param name="request">The canonical request to translate.</param>
        /// <returns>The translated canonical request.</returns>
        CanonicalRequest Translate(CanonicalRequest request);
    }
}
