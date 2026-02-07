// <copyright file="ITokenVault.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Security
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides encryption and decryption services for sensitive tokens.
    /// </summary>
    public interface ITokenVault
    {
        /// <summary>
        /// Encrypts plaintext for a specific tenant.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="plaintext">The plaintext to encrypt.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The encrypted ciphertext.</returns>
        Task<byte[]> EncryptAsync(Guid tenantId, string plaintext, CancellationToken cancellationToken = default);

        /// <summary>
        /// Decrypts ciphertext for a specific tenant.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="ciphertext">The ciphertext to decrypt.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The decrypted plaintext.</returns>
        Task<string> DecryptAsync(Guid tenantId, byte[] ciphertext, CancellationToken cancellationToken = default);

        /// <summary>
        /// Rotates the encryption key for a specific tenant.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="newKeyBase64">The new encryption key in Base64 format.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RotateKeyAsync(Guid tenantId, string newKeyBase64, CancellationToken cancellationToken = default);
    }
}
