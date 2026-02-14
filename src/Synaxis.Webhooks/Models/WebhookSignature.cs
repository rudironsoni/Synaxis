// <copyright file="WebhookSignature.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Webhooks.Models
{
    using System;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// Provides HMAC-SHA256 signature generation and verification for webhooks.
    /// </summary>
    public static class WebhookSignature
    {
        private const string SignatureHeader = "X-Webhook-Signature";
        private const string SignaturePrefix = "sha256=";

        /// <summary>
        /// Generates an HMAC-SHA256 signature for the given payload.
        /// </summary>
        /// <param name="payload">The payload to sign.</param>
        /// <param name="secret">The secret key used for signing.</param>
        /// <returns>The HMAC-SHA256 signature.</returns>
        public static string GenerateSignature(string payload, string secret)
        {
            if (string.IsNullOrEmpty(payload))
            {
                throw new ArgumentException("Payload cannot be null or empty.", nameof(payload));
            }

            if (string.IsNullOrEmpty(secret))
            {
                throw new ArgumentException("Secret cannot be null or empty.", nameof(secret));
            }

            var keyBytes = Encoding.UTF8.GetBytes(secret);
            var payloadBytes = Encoding.UTF8.GetBytes(payload);

            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(payloadBytes);
            var signature = Convert.ToHexString(hashBytes).ToLowerInvariant();

            return $"{SignaturePrefix}{signature}";
        }

        /// <summary>
        /// Verifies the HMAC-SHA256 signature of a payload.
        /// </summary>
        /// <param name="payload">The payload to verify.</param>
        /// <param name="signature">The signature to verify.</param>
        /// <param name="secret">The secret key used for verification.</param>
        /// <returns>True if the signature is valid; otherwise, false.</returns>
        public static bool VerifySignature(string payload, string signature, string secret)
        {
            if (string.IsNullOrEmpty(payload))
            {
                return false;
            }

            if (string.IsNullOrEmpty(signature))
            {
                return false;
            }

            if (string.IsNullOrEmpty(secret))
            {
                return false;
            }

            try
            {
                var expectedSignature = GenerateSignature(payload, secret);

                // Use constant-time comparison to prevent timing attacks
                return CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(signature),
                    Encoding.UTF8.GetBytes(expectedSignature));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the signature header name.
        /// </summary>
        /// <returns>The signature header name.</returns>
        public static string GetSignatureHeader()
        {
            return SignatureHeader;
        }

        /// <summary>
        /// Extracts the signature value from a signature header value.
        /// </summary>
        /// <param name="signatureHeader">The signature header value.</param>
        /// <returns>The signature value without the prefix.</returns>
        public static string ExtractSignature(string signatureHeader)
        {
            if (string.IsNullOrEmpty(signatureHeader))
            {
                return null;
            }

            if (signatureHeader.StartsWith(SignaturePrefix, StringComparison.OrdinalIgnoreCase))
            {
                return signatureHeader.Substring(SignaturePrefix.Length);
            }

            return signatureHeader;
        }
    }
}
