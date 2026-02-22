// <copyright file="EncryptedData.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Encryption;

/// <summary>
/// Represents encrypted data with all necessary components for decryption.
/// </summary>
/// <remarks>
/// This value object follows the envelope encryption pattern where the data
/// encryption key (DEK) is encrypted with a key encryption key (KEK) from the key vault.
/// </remarks>
public sealed record EncryptedData
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptedData"/> class.
    /// </summary>
    /// <param name="ciphertext">The encrypted data.</param>
    /// <param name="encryptedKey">The encrypted data encryption key.</param>
    /// <param name="nonce">The nonce/IV used for encryption.</param>
    /// <param name="tag">The authentication tag for GCM mode.</param>
    /// <param name="algorithm">The encryption algorithm used.</param>
    /// <param name="keyId">The identifier of the key used for encryption.</param>
    /// <param name="keyVersion">The version of the key used.</param>
    public EncryptedData(
        byte[] ciphertext,
        byte[] encryptedKey,
        byte[] nonce,
        byte[] tag,
        string algorithm,
        string keyId,
        string keyVersion)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);
        this.Ciphertext = ciphertext;
        ArgumentNullException.ThrowIfNull(encryptedKey);
        this.EncryptedKey = encryptedKey;
        ArgumentNullException.ThrowIfNull(nonce);
        this.Nonce = nonce;
        ArgumentNullException.ThrowIfNull(tag);
        this.Tag = tag;
        ArgumentNullException.ThrowIfNull(algorithm);
        this.Algorithm = algorithm;
        ArgumentNullException.ThrowIfNull(keyId);
        this.KeyId = keyId;
        ArgumentNullException.ThrowIfNull(keyVersion);
        this.KeyVersion = keyVersion;
    }

    /// <summary>
    /// Gets the encrypted data.
    /// </summary>
    public byte[] Ciphertext { get; }

    /// <summary>
    /// Gets the encrypted data encryption key (DEK).
    /// </summary>
    /// <remarks>
    /// The DEK is encrypted with the key encryption key (KEK) from the key vault.
    /// </remarks>
    public byte[] EncryptedKey { get; }

    /// <summary>
    /// Gets the nonce/IV used for encryption.
    /// </summary>
    public byte[] Nonce { get; }

    /// <summary>
    /// Gets the authentication tag for GCM mode.
    /// </summary>
    public byte[] Tag { get; }

    /// <summary>
    /// Gets the encryption algorithm used.
    /// </summary>
    public string Algorithm { get; }

    /// <summary>
    /// Gets the identifier of the key used for encryption.
    /// </summary>
    public string KeyId { get; }

    /// <summary>
    /// Gets the version of the key used.
    /// </summary>
    public string KeyVersion { get; }

    /// <summary>
    /// Creates a new instance with updated key information (for key rotation).
    /// </summary>
    /// <param name="newEncryptedKey">The new encrypted key.</param>
    /// <param name="newKeyId">The new key identifier.</param>
    /// <param name="newKeyVersion">The new key version.</param>
    /// <returns>A new <see cref="EncryptedData"/> instance with updated key information.</returns>
    public EncryptedData WithNewKey(
        byte[] newEncryptedKey,
        string newKeyId,
        string newKeyVersion)
    {
        return new EncryptedData(
            this.Ciphertext,
            newEncryptedKey,
            this.Nonce,
            this.Tag,
            this.Algorithm,
            newKeyId,
            newKeyVersion);
    }
}
