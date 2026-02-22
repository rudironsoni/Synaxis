// <copyright file="TenantKeyService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Encryption;

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// Manages tenant encryption keys with support for multiple keys per tenant/user/provider.
/// </summary>
public sealed class TenantKeyService
{
    private readonly IKeyVault _keyVault;
    private readonly ILogger<TenantKeyService> _logger;
    private readonly EncryptionOptions _options;
    private readonly ConcurrentDictionary<string, TenantKeyMetadata> _keyCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantKeyService"/> class.
    /// </summary>
    /// <param name="keyVault">The key vault service.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="options">The encryption options.</param>
    public TenantKeyService(
        IKeyVault keyVault,
        ILogger<TenantKeyService> logger,
        IOptions<EncryptionOptions> options)
    {
        ArgumentNullException.ThrowIfNull(keyVault);
        this._keyVault = keyVault;
        ArgumentNullException.ThrowIfNull(logger);
        this._logger = logger;
        ArgumentNullException.ThrowIfNull(options);
        this._options = options.Value;
        this._keyCache = new ConcurrentDictionary<string, TenantKeyMetadata>(StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets or creates a tenant key.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="providerId">The provider identifier (optional).</param>
    /// <param name="userId">The user identifier (optional).</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The tenant key metadata.</returns>
    public async Task<TenantKeyMetadata> GetOrCreateKeyAsync(
        string tenantId,
        string? providerId = null,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        var keyName = BuildKeyName(tenantId, providerId, userId);

        if (this._keyCache.TryGetValue(keyName, out var cachedKey))
        {
            return cachedKey;
        }

        // Check if key exists in vault
        var secretName = BuildSecretName(tenantId, providerId, userId);
        var secretValue = await this._keyVault.GetSecretAsync(secretName, cancellationToken).ConfigureAwait(false);

        TenantKeyMetadata metadata;

        if (secretValue != null)
        {
            metadata = TenantKeyMetadata.FromJson(secretValue);
            this._keyCache[keyName] = metadata;
            this._logger.LogDebug("Retrieved existing key {KeyName} for tenant {TenantId}", keyName, tenantId);
            return metadata;
        }

        // Create new key
        metadata = await this.CreateKeyAsync(tenantId, providerId, userId, cancellationToken).ConfigureAwait(false);
        this._keyCache[keyName] = metadata;
        this._logger.LogInformation("Created new key {KeyName} for tenant {TenantId}", keyName, tenantId);
        return metadata;
    }

    /// <summary>
    /// Rotates a tenant key.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="providerId">The provider identifier (optional).</param>
    /// <param name="userId">The user identifier (optional).</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The new tenant key metadata.</returns>
    public async Task<TenantKeyMetadata> RotateKeyAsync(
        string tenantId,
        string? providerId = null,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        var keyName = BuildKeyName(tenantId, providerId, userId);
        var secretName = BuildSecretName(tenantId, providerId, userId);

        // Get current key
        var secretValue = await this._keyVault.GetSecretAsync(secretName, cancellationToken).ConfigureAwait(false);
        if (secretValue == null)
        {
            throw new InvalidOperationException($"Key {keyName} does not exist");
        }

        var currentMetadata = TenantKeyMetadata.FromJson(secretValue);

        // Create new version
        var newMetadata = await this.CreateKeyAsync(tenantId, providerId, userId, cancellationToken).ConfigureAwait(false);

        // Update cache
        this._keyCache[keyName] = newMetadata;

        this._logger.LogInformation(
            "Rotated key {KeyName} from version {OldVersion} to {NewVersion}",
            keyName,
            currentMetadata.Version,
            newMetadata.Version);

        return newMetadata;
    }

    /// <summary>
    /// Deactivates a tenant key.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="providerId">The provider identifier (optional).</param>
    /// <param name="userId">The user identifier (optional).</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DeactivateKeyAsync(
        string tenantId,
        string? providerId = null,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        var keyName = BuildKeyName(tenantId, providerId, userId);
        var secretName = BuildSecretName(tenantId, providerId, userId);

        var secretValue = await this._keyVault.GetSecretAsync(secretName, cancellationToken).ConfigureAwait(false);
        if (secretValue == null)
        {
            throw new InvalidOperationException($"Key {keyName} does not exist");
        }

        var metadata = TenantKeyMetadata.FromJson(secretValue);
        metadata = metadata with { IsActive = false };

        await this._keyVault.SetSecretAsync(secretName, metadata.ToJson(), cancellationToken).ConfigureAwait(false);
        this._keyCache.TryRemove(keyName, out _);

        this._logger.LogInformation("Deactivated key {KeyName}", keyName);
    }

    /// <summary>
    /// Builds a key name for the given tenant, provider, and user.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="providerId">The provider identifier (optional).</param>
    /// <param name="userId">The user identifier (optional).</param>
    /// <returns>The key name.</returns>
    public static string BuildKeyName(
        string tenantId,
        string? providerId = null,
        string? userId = null)
    {
        var parts = new List<string> { "tenant", tenantId };

        if (!string.IsNullOrWhiteSpace(providerId))
        {
            parts.Add("provider");
            parts.Add(providerId);
        }

        if (!string.IsNullOrWhiteSpace(userId))
        {
            parts.Add("user");
            parts.Add(userId);
        }

        return string.Join("-", parts);
    }

    private static string BuildSecretName(
        string tenantId,
        string? providerId = null,
        string? userId = null)
    {
        return $"encryption-{BuildKeyName(tenantId, providerId, userId)}";
    }

    private async Task<TenantKeyMetadata> CreateKeyAsync(
        string tenantId,
        string? providerId,
        string? userId,
        CancellationToken cancellationToken)
    {
        var keyName = BuildKeyName(tenantId, providerId, userId);
        var secretName = BuildSecretName(tenantId, providerId, userId);

        // Generate a new master key for this tenant
        var masterKey = AesEncryptionProvider.GenerateDataKey();

        // Store the master key in the vault
        var masterKeyBase64 = Convert.ToBase64String(masterKey);
        await this._keyVault.SetSecretAsync(secretName, masterKeyBase64, cancellationToken).ConfigureAwait(false);

        // Create metadata
        var metadata = new TenantKeyMetadata(
            KeyId: keyName,
            Version: Guid.NewGuid().ToString(),
            CreatedAt: DateTime.UtcNow,
            IsActive: true,
            Algorithm: this._options.DefaultAlgorithm);

        // Store metadata separately
        var metadataSecretName = $"{secretName}-metadata";
        await this._keyVault.SetSecretAsync(metadataSecretName, metadata.ToJson(), cancellationToken).ConfigureAwait(false);

        return metadata;
    }
}
