// <copyright file="EncryptionService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Encryption;

using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// Implements envelope encryption using AES-256-GCM.
/// </summary>
/// <remarks>
/// This service follows the envelope encryption pattern:
/// 1. Generate a data encryption key (DEK).
/// 2. Encrypt the DEK with the tenant's key encryption key (KEK) from the key vault.
/// 3. Encrypt the data with the DEK.
/// 4. Store the encrypted data along with the encrypted DEK.
/// </remarks>
public sealed class EncryptionService : IEncryptionService
{
    private readonly IKeyVault _keyVault;
    private readonly TenantKeyService _tenantKeyService;
    private readonly ILogger<EncryptionService> _logger;
    private readonly EncryptionOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptionService"/> class.
    /// </summary>
    /// <param name="keyVault">The key vault service.</param>
    /// <param name="tenantKeyService">The tenant key service.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="options">The encryption options.</param>
    public EncryptionService(
        IKeyVault keyVault,
        TenantKeyService tenantKeyService,
        ILogger<EncryptionService> logger,
        IOptions<EncryptionOptions> options)
    {
        this._keyVault = keyVault ?? throw new ArgumentNullException(nameof(keyVault));
        this._tenantKeyService = tenantKeyService ?? throw new ArgumentNullException(nameof(tenantKeyService));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this._options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        this._options.Validate();
    }

    /// <inheritdoc/>
    public async Task<EncryptedData> EncryptAsync(
        byte[] plaintext,
        string tenantId,
        string keyId,
        CancellationToken cancellationToken = default)
    {
        if (plaintext == null)
        {
            throw new ArgumentNullException(nameof(plaintext));
        }

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(keyId))
        {
            throw new ArgumentException("Key ID cannot be empty", nameof(keyId));
        }

        this._logger.LogDebug(
            "Encrypting data for tenant {TenantId} with key {KeyId}",
            tenantId,
            keyId);

        // Get or create tenant key metadata
        var keyMetadata = await this._tenantKeyService.GetOrCreateKeyAsync(
            tenantId,
            keyId,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        // Generate data encryption key (DEK)
        var dek = AesEncryptionProvider.GenerateDataKey();

        // Generate nonce
        var nonce = AesEncryptionProvider.GenerateNonce();

        // Encrypt data with DEK
        var (ciphertext, tag) = AesEncryptionProvider.Encrypt(plaintext, dek, nonce);

        // Encrypt DEK with KEK from KeyVault
        var secretName = TenantKeyService.BuildKeyName(tenantId, keyId);
        var encryptedDek = await this._keyVault.EncryptAsync(secretName, dek, cancellationToken).ConfigureAwait(false);

        var encryptedData = new EncryptedData(
            ciphertext: ciphertext,
            encryptedKey: encryptedDek,
            nonce: nonce,
            tag: tag,
            algorithm: this._options.DefaultAlgorithm,
            keyId: keyMetadata.KeyId,
            keyVersion: keyMetadata.Version);

        this._logger.LogDebug(
            "Successfully encrypted data for tenant {TenantId} with key {KeyId}",
            tenantId,
            keyId);

        return encryptedData;
    }

    /// <inheritdoc/>
    public async Task<byte[]> DecryptAsync(
        EncryptedData encryptedData,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        if (encryptedData == null)
        {
            throw new ArgumentNullException(nameof(encryptedData));
        }

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant ID cannot be empty", nameof(tenantId));
        }

        this._logger.LogDebug(
            "Decrypting data for tenant {TenantId} with key {KeyId}",
            tenantId,
            encryptedData.KeyId);

        // Decrypt DEK with KEK from KeyVault
        var secretName = TenantKeyService.BuildKeyName(tenantId, encryptedData.KeyId);
        var decryptedDek = await this._keyVault.DecryptAsync(
            secretName,
            encryptedData.EncryptedKey,
            cancellationToken).ConfigureAwait(false);

        // Decrypt data with DEK
        var plaintext = AesEncryptionProvider.Decrypt(
            encryptedData.Ciphertext,
            encryptedData.Tag,
            decryptedDek,
            encryptedData.Nonce);

        this._logger.LogDebug(
            "Successfully decrypted data for tenant {TenantId} with key {KeyId}",
            tenantId,
            encryptedData.KeyId);

        return plaintext;
    }

    /// <inheritdoc/>
    public Task<EncryptedData> EncryptStringAsync(
        string plaintext,
        string tenantId,
        string keyId,
        CancellationToken cancellationToken = default)
    {
        if (plaintext == null)
        {
            throw new ArgumentNullException(nameof(plaintext));
        }

        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        return this.EncryptAsync(plaintextBytes, tenantId, keyId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<string> DecryptToStringAsync(
        EncryptedData encryptedData,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        var plaintextBytes = await this.DecryptAsync(encryptedData, tenantId, cancellationToken).ConfigureAwait(false);
        return Encoding.UTF8.GetString(plaintextBytes);
    }
}
