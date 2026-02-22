// <copyright file="AesEncryptionProvider.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Encryption;

using System.Security.Cryptography;

/// <summary>
/// Provides AES-256-GCM encryption functionality.
/// </summary>
public static class AesEncryptionProvider
{
    private const int KeySizeBytes = 32; // 256 bits
    private const int NonceSizeBytes = 12; // 96 bits for GCM
    private const int TagSizeBytes = 16; // 128 bits for GCM

    /// <summary>
    /// Generates a new data encryption key.
    /// </summary>
    /// <returns>A randomly generated 256-bit key.</returns>
    public static byte[] GenerateDataKey()
    {
        return RandomNumberGenerator.GetBytes(KeySizeBytes);
    }

    /// <summary>
    /// Generates a new nonce for GCM encryption.
    /// </summary>
    /// <returns>A randomly generated 96-bit nonce.</returns>
    public static byte[] GenerateNonce()
    {
        return RandomNumberGenerator.GetBytes(NonceSizeBytes);
    }

    /// <summary>
    /// Encrypts data using AES-256-GCM.
    /// </summary>
    /// <param name="plaintext">The plaintext data to encrypt.</param>
    /// <param name="key">The encryption key (32 bytes for AES-256).</param>
    /// <param name="nonce">The nonce (12 bytes for GCM).</param>
    /// <returns>A tuple containing the ciphertext and authentication tag.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    /// <exception cref="ArgumentException">Thrown when key or nonce has incorrect length.</exception>
    public static (byte[] Ciphertext, byte[] Tag) Encrypt(
        byte[] plaintext,
        byte[] key,
        byte[] nonce)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(nonce);
        if (key.Length != KeySizeBytes)
        {
            throw new ArgumentException($"Key must be {KeySizeBytes} bytes for AES-256", nameof(key));
        }

        if (nonce.Length != NonceSizeBytes)
        {
            throw new ArgumentException($"Nonce must be {NonceSizeBytes} bytes for GCM", nameof(nonce));
        }

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSizeBytes];

        using var aes = new AesGcm(key, TagSizeBytes);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        return (ciphertext, tag);
    }

    /// <summary>
    /// Decrypts data using AES-256-GCM.
    /// </summary>
    /// <param name="ciphertext">The ciphertext to decrypt.</param>
    /// <param name="tag">The authentication tag.</param>
    /// <param name="key">The decryption key (32 bytes for AES-256).</param>
    /// <param name="nonce">The nonce (12 bytes for GCM).</param>
    /// <returns>The decrypted plaintext data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    /// <exception cref="ArgumentException">Thrown when key or nonce has incorrect length.</exception>
    /// <exception cref="CryptographicException">Thrown when authentication fails.</exception>
    public static byte[] Decrypt(
        byte[] ciphertext,
        byte[] tag,
        byte[] key,
        byte[] nonce)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);
        ArgumentNullException.ThrowIfNull(tag);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(nonce);
        if (key.Length != KeySizeBytes)
        {
            throw new ArgumentException($"Key must be {KeySizeBytes} bytes for AES-256", nameof(key));
        }

        if (nonce.Length != NonceSizeBytes)
        {
            throw new ArgumentException($"Nonce must be {NonceSizeBytes} bytes for GCM", nameof(nonce));
        }

        if (tag.Length != TagSizeBytes)
        {
            throw new ArgumentException($"Tag must be {TagSizeBytes} bytes for GCM", nameof(tag));
        }

        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, TagSizeBytes);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return plaintext;
    }
}
