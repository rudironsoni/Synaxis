// <copyright file="ApiKeyService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Security
{
    using System.Security.Cryptography;
    using System.Text;
    using Synaxis.InferenceGateway.Application.Security;

    /// <summary>
    /// ApiKeyService class.
    /// </summary>
    public sealed class ApiKeyService : IApiKeyService
    {
        private const string Prefix = "sk-synaxis-";

        /// <summary>
        /// Generates a new API key with the Synaxis prefix.
        /// </summary>
        /// <returns>A newly generated API key string.</returns>
        public string GenerateKey()
        {
            var bytes = new byte[16];
            RandomNumberGenerator.Fill(bytes);

            // Use base62 or hex. Hex is simpler.
            var hex = Convert.ToHexString(bytes).ToLowerInvariant();
            return $"{Prefix}{hex}";
        }

        /// <summary>
        /// Hashes an API key using SHA256.
        /// </summary>
        /// <param name="key">The API key to hash.</param>
        /// <returns>A Base64-encoded hash of the key.</returns>
        public string HashKey(string key)
        {
            var bytes = Encoding.UTF8.GetBytes(key);
            var hash = SHA256.HashData(bytes);
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Validates an API key against a stored hash using constant-time comparison.
        /// </summary>
        /// <param name="key">The API key to validate.</param>
        /// <param name="hash">The stored hash to compare against.</param>
        /// <returns>True if the key matches the hash; otherwise, false.</returns>
        public bool ValidateKey(string key, string hash)
        {
            var computed = this.HashKey(key);

            // Constant time comparison
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computed),
                Encoding.UTF8.GetBytes(hash));
        }
    }
}
