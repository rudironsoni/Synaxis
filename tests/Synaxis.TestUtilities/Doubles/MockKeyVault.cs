// <copyright file="MockKeyVault.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.TestUtilities.Doubles;

using System.Collections.Concurrent;
using System.Security.Cryptography;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// A mock implementation of <see cref="IKeyVault"/> for testing purposes.
/// Uses in-memory storage with thread-safe operations.
/// </summary>
public sealed class MockKeyVault : IKeyVault
{
    private readonly ConcurrentDictionary<string, string> _secrets = new();
    private readonly ConcurrentDictionary<string, byte[]> _keys = new();

    /// <summary>
    /// Stores a secret in the key vault.
    /// </summary>
    /// <param name="secretName">The name of the secret to store.</param>
    /// <param name="secretValue">The value of the secret to store.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task SetSecretAsync(
        string secretName,
        string secretValue,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(secretName);
        ArgumentNullException.ThrowIfNull(secretValue);

        cancellationToken.ThrowIfCancellationRequested();

        _secrets[secretName] = secretValue;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Retrieves a secret from the key vault.
    /// </summary>
    /// <param name="secretName">The name of the secret to retrieve.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The secret value, or null if not found.</returns>
    public Task<string?> GetSecretAsync(
        string secretName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(secretName);

        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(_secrets.TryGetValue(secretName, out var value) ? value : null);
    }

    /// <summary>
    /// Deletes a secret from the key vault.
    /// </summary>
    /// <param name="secretName">The name of the secret to delete.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task DeleteSecretAsync(
        string secretName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(secretName);

        cancellationToken.ThrowIfCancellationRequested();

        _secrets.TryRemove(secretName, out _);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Encrypts data using a key from the vault.
    /// </summary>
    /// <param name="keyName">The name of the encryption key.</param>
    /// <param name="plaintext">The plaintext data to encrypt.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The encrypted data as a byte array.</returns>
    public Task<byte[]> EncryptAsync(
        string keyName,
        byte[] plaintext,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(keyName);
        ArgumentNullException.ThrowIfNull(plaintext);

        cancellationToken.ThrowIfCancellationRequested();

        // Generate or retrieve key
        var key = _keys.GetOrAdd(keyName, _ => GenerateKey());

        // Simple XOR encryption for testing purposes
        var encrypted = new byte[plaintext.Length];
        for (var i = 0; i < plaintext.Length; i++)
        {
            encrypted[i] = (byte)(plaintext[i] ^ key[i % key.Length]);
        }

        return Task.FromResult(encrypted);
    }

    /// <summary>
    /// Decrypts data using a key from the vault.
    /// </summary>
    /// <param name="keyName">The name of the decryption key.</param>
    /// <param name="ciphertext">The encrypted data to decrypt.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The decrypted plaintext data as a byte array.</returns>
    public Task<byte[]> DecryptAsync(
        string keyName,
        byte[] ciphertext,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(keyName);
        ArgumentNullException.ThrowIfNull(ciphertext);

        cancellationToken.ThrowIfCancellationRequested();

        if (!_keys.TryGetValue(keyName, out var key))
        {
            throw new InvalidOperationException($"Key '{keyName}' not found.");
        }

        // Simple XOR decryption (same operation as encryption)
        var decrypted = new byte[ciphertext.Length];
        for (var i = 0; i < ciphertext.Length; i++)
        {
            decrypted[i] = (byte)(ciphertext[i] ^ key[i % key.Length]);
        }

        return Task.FromResult(decrypted);
    }

    /// <summary>
    /// Clears all stored secrets and keys.
    /// </summary>
    public void Clear()
    {
        _secrets.Clear();
        _keys.Clear();
    }

    /// <summary>
    /// Gets all secret names.
    /// </summary>
    /// <returns>A collection of secret names.</returns>
    public IEnumerable<string> GetSecretNames() => _secrets.Keys.ToList();

    /// <summary>
    /// Checks if a secret exists.
    /// </summary>
    /// <param name="secretName">The secret name to check.</param>
    /// <returns>True if the secret exists; otherwise, false.</returns>
    public bool SecretExists(string secretName) => _secrets.ContainsKey(secretName);

    /// <summary>
    /// Gets the count of stored secrets.
    /// </summary>
    /// <returns>The number of secrets.</returns>
    public int SecretCount => _secrets.Count;

    private static byte[] GenerateKey()
    {
        var key = new byte[32];
        RandomNumberGenerator.Fill(key);
        return key;
    }
}
