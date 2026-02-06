// <copyright file="IApiKeyService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Security
{
    /// <summary>
    /// Provides API key generation and validation services.
    /// </summary>
    public interface IApiKeyService
    {
        /// <summary>
        /// Generates a new API key.
        /// </summary>
        /// <returns>The generated API key.</returns>
        string GenerateKey();

        /// <summary>
        /// Hashes an API key for secure storage.
        /// </summary>
        /// <param name="key">The API key to hash.</param>
        /// <returns>The hashed key.</returns>
        string HashKey(string key);

        /// <summary>
        /// Validates an API key against its hash.
        /// </summary>
        /// <param name="key">The API key to validate.</param>
        /// <param name="hash">The stored hash to validate against.</param>
        /// <returns>True if the key is valid, otherwise false.</returns>
        bool ValidateKey(string key, string hash);
    }
}