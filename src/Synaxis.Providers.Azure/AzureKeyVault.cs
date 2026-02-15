// <copyright file="AzureKeyVault.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

using System;
using System.Threading;
using System.Threading.Tasks;
using global::Azure;
using global::Azure.Identity;
using global::Azure.Security.KeyVault.Keys;
using global::Azure.Security.KeyVault.Keys.Cryptography;
using global::Azure.Security.KeyVault.Secrets;
using global::Polly;
using Microsoft.Extensions.Logging;
using Synaxis.Abstractions.Cloud;

namespace Synaxis.Providers.Azure;

/// <summary>
/// Azure Key Vault implementation of IKeyVault.
/// </summary>
public class AzureKeyVault : IKeyVault
{
    private readonly SecretClient _secretClient;
    private readonly KeyClient _keyClient;
    private readonly ILogger<AzureKeyVault> _logger;
    private readonly IAsyncPolicy _retryPolicy;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureKeyVault"/> class.
    /// </summary>
    /// <param name="keyVaultUrl">The URL of the Azure Key Vault.</param>
    /// <param name="credential">The Azure credential for authentication.</param>
    /// <param name="logger">The logger instance.</param>
    public AzureKeyVault(
        string keyVaultUrl,
        DefaultAzureCredential credential,
        ILogger<AzureKeyVault> logger)
    {
        if (string.IsNullOrWhiteSpace(keyVaultUrl))
        {
            throw new ArgumentException("Key Vault URL cannot be null or empty.", nameof(keyVaultUrl));
        }

        _secretClient = new SecretClient(new Uri(keyVaultUrl), credential);
        _keyClient = new KeyClient(new Uri(keyVaultUrl), credential);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _retryPolicy = Policy
            .Handle<global::Azure.RequestFailedException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "Retry {RetryCount} after {Delay}s",
                        retryCount,
                        timespan.TotalSeconds);
                });
    }

    /// <inheritdoc />
    public async Task SetSecretAsync(
        string secretName,
        string secretValue,
        CancellationToken cancellationToken = default)
    {
        await _retryPolicy.ExecuteAsync(async () =>
        {
            await _secretClient.SetSecretAsync(secretName, secretValue, cancellationToken);
            _logger.LogInformation("Secret {SecretName} stored successfully", secretName);
        });
    }

    /// <inheritdoc />
    public async Task<string?> GetSecretAsync(
        string secretName,
        CancellationToken cancellationToken = default)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            try
            {
                var response = await _secretClient.GetSecretAsync(secretName, cancellationToken: cancellationToken);
                _logger.LogInformation("Secret {SecretName} retrieved successfully", secretName);
                return response.Value.Value;
            }
            catch (global::Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogWarning("Secret {SecretName} not found", secretName);
                return null;
            }
        });
    }

    /// <inheritdoc />
    public async Task DeleteSecretAsync(
        string secretName,
        CancellationToken cancellationToken = default)
    {
        await _retryPolicy.ExecuteAsync(async () =>
        {
            await _secretClient.StartDeleteSecretAsync(secretName, cancellationToken);
            _logger.LogInformation("Secret {SecretName} deletion started", secretName);
        });
    }

    /// <inheritdoc />
    public async Task<byte[]> EncryptAsync(
        string keyName,
        byte[] plaintext,
        CancellationToken cancellationToken = default)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var keyResponse = await _keyClient.GetKeyAsync(keyName, cancellationToken: cancellationToken);
            var cryptoClient = new CryptographyClient(keyResponse.Value.Id, new DefaultAzureCredential());

            var encryptResult = await cryptoClient.EncryptAsync(
                EncryptionAlgorithm.RsaOaep256,
                plaintext,
                cancellationToken);

            _logger.LogInformation("Data encrypted successfully using key {KeyName}", keyName);
            return encryptResult.Ciphertext;
        });
    }

    /// <inheritdoc />
    public async Task<byte[]> DecryptAsync(
        string keyName,
        byte[] ciphertext,
        CancellationToken cancellationToken = default)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var keyResponse = await _keyClient.GetKeyAsync(keyName, cancellationToken: cancellationToken);
            var cryptoClient = new CryptographyClient(keyResponse.Value.Id, new DefaultAzureCredential());

            var decryptResult = await cryptoClient.DecryptAsync(
                EncryptionAlgorithm.RsaOaep256,
                ciphertext,
                cancellationToken);

            _logger.LogInformation("Data decrypted successfully using key {KeyName}", keyName);
            return decryptResult.Plaintext;
        });
    }

    /// <summary>
    /// Creates a tenant-scoped key name for multi-tenant support.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="keyId">The key identifier.</param>
    /// <returns>The tenant-scoped key name.</returns>
    public static string GetTenantKeyName(string tenantId, string keyId)
    {
        return $"tenant-{tenantId}-{keyId}";
    }

    /// <summary>
    /// Creates a tenant-scoped secret name for multi-tenant support.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="secretName">The secret name.</param>
    /// <returns>The tenant-scoped secret name.</returns>
    public static string GetTenantSecretName(string tenantId, string secretName)
    {
        return $"tenant-{tenantId}-{secretName}";
    }
}
