// <copyright file="IKeyVault.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Abstractions.Cloud;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Defines a contract for managing encryption keys and secrets.
/// </summary>
public interface IKeyVault
{
    /// <summary>
    /// Stores a secret in the key vault.
    /// </summary>
    /// <param name="secretName">The name of the secret to store.</param>
    /// <param name="secretValue">The value of the secret to store.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SetSecretAsync(
        string secretName,
        string secretValue,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a secret from the key vault.
    /// </summary>
    /// <param name="secretName">The name of the secret to retrieve.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The secret value, or null if not found.</returns>
    Task<string?> GetSecretAsync(
        string secretName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a secret from the key vault.
    /// </summary>
    /// <param name="secretName">The name of the secret to delete.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteSecretAsync(
        string secretName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Encrypts data using a key from the vault.
    /// </summary>
    /// <param name="keyName">The name of the encryption key.</param>
    /// <param name="plaintext">The plaintext data to encrypt.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The encrypted data as a byte array.</returns>
    Task<byte[]> EncryptAsync(
        string keyName,
        byte[] plaintext,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts data using a key from the vault.
    /// </summary>
    /// <param name="keyName">The name of the decryption key.</param>
    /// <param name="ciphertext">The encrypted data to decrypt.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The decrypted plaintext data as a byte array.</returns>
    Task<byte[]> DecryptAsync(
        string keyName,
        byte[] ciphertext,
        CancellationToken cancellationToken = default);
}
