// <copyright file="IPasswordHasher.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Security
{
    /// <summary>
    /// Provides password hashing and verification services.
    /// </summary>
    public interface IPasswordHasher
    {
        /// <summary>
        /// Hashes a password using a secure hashing algorithm.
        /// </summary>
        /// <param name="password">The plaintext password to hash.</param>
        /// <returns>The hashed password.</returns>
        string HashPassword(string password);

        /// <summary>
        /// Verifies a password against a hash.
        /// </summary>
        /// <param name="password">The plaintext password to verify.</param>
        /// <param name="hash">The hash to verify against.</param>
        /// <returns>True if the password matches the hash; otherwise, false.</returns>
        bool VerifyPassword(string password, string hash);
    }
}
