// <copyright file="IEncryptionService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Encryption;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Defines a contract for encrypting and decrypting data using envelope encryption.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts data using envelope encryption.
    /// </summary>
    /// <param name="plaintext">The plaintext data to encrypt.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="keyId">The key identifier.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The encrypted data.</returns>
    /// <remarks>
    /// This method uses envelope encryption:
    /// 1. Generates a data encryption key (DEK).
    /// 2. Encrypts the DEK with the tenant's key encryption key (KEK).
    /// 3. Encrypts the data with the DEK.
    /// 4. Returns the encrypted data along with the encrypted DEK.
    /// </remarks>
    Task<EncryptedData> EncryptAsync(
        byte[] plaintext,
        string tenantId,
        string keyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts data that was encrypted using envelope encryption.
    /// </summary>
    /// <param name="encryptedData">The encrypted data to decrypt.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The decrypted plaintext data.</returns>
    /// <remarks>
    /// This method reverses the envelope encryption process:
    /// 1. Decrypts the DEK using the tenant's KEK.
    /// 2. Decrypts the data using the DEK.
    /// </remarks>
    Task<byte[]> DecryptAsync(
        EncryptedData encryptedData,
        string tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Encrypts a string value.
    /// </summary>
    /// <param name="plaintext">The plaintext string to encrypt.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="keyId">The key identifier.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The encrypted data.</returns>
    Task<EncryptedData> EncryptStringAsync(
        string plaintext,
        string tenantId,
        string keyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts data to a string value.
    /// </summary>
    /// <param name="encryptedData">The encrypted data to decrypt.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The decrypted plaintext string.</returns>
    Task<string> DecryptToStringAsync(
        EncryptedData encryptedData,
        string tenantId,
        CancellationToken cancellationToken = default);
}
