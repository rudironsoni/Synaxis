// <copyright file="RedisKeyVault.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.OnPrem;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Synaxis.Shared.Kernel.Application.Cloud;

/// <summary>
/// Redis-based implementation of IKeyVault for on-premise deployments.
/// Provides simple secret storage with basic encryption capabilities.
/// </summary>
public class RedisKeyVault : IKeyVault
{
    private readonly ILogger<RedisKeyVault> _logger;
    private readonly IDatabase _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisKeyVault"/> class.
    /// </summary>
    /// <param name="connectionString">The Redis connection string.</param>
    /// <param name="logger">The logger instance.</param>
    public RedisKeyVault(
        string connectionString,
        ILogger<RedisKeyVault> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        this._logger = logger;
        this._database = ConnectionMultiplexer.Connect(connectionString).GetDatabase();
    }

    /// <inheritdoc />
    public async Task SetSecretAsync(
        string secretName,
        string secretValue,
        CancellationToken cancellationToken = default)
    {
        var key = GetSecretKey(secretName);
        await this._database.StringSetAsync(key, secretValue).ConfigureAwait(false);
        this._logger.LogInformation("Secret {SecretName} stored successfully", secretName);
    }

    /// <inheritdoc />
    public async Task<string?> GetSecretAsync(
        string secretName,
        CancellationToken cancellationToken = default)
    {
        var key = GetSecretKey(secretName);
        var value = await this._database.StringGetAsync(key).ConfigureAwait(false);

        if (value.IsNull)
        {
            this._logger.LogWarning("Secret {SecretName} not found", secretName);
            return null;
        }

        this._logger.LogInformation("Secret {SecretName} retrieved successfully", secretName);
        return value;
    }

    /// <inheritdoc />
    public async Task DeleteSecretAsync(
        string secretName,
        CancellationToken cancellationToken = default)
    {
        var key = GetSecretKey(secretName);
        await this._database.KeyDeleteAsync(key).ConfigureAwait(false);
        this._logger.LogInformation("Secret {SecretName} deleted successfully", secretName);
    }

    /// <inheritdoc />
    public Task<byte[]> EncryptAsync(
        string keyName,
        byte[] plaintext,
        CancellationToken cancellationToken = default)
    {
        // Simple implementation: store as-is (in production, use proper encryption)
        // This is a stub that demonstrates the interface
        this._logger.LogWarning("Encryption is not implemented in RedisKeyVault. Data stored as-is.");
        return Task.FromResult(plaintext);
    }

    /// <inheritdoc />
    public Task<byte[]> DecryptAsync(
        string keyName,
        byte[] ciphertext,
        CancellationToken cancellationToken = default)
    {
        // Simple implementation: return as-is (in production, use proper decryption)
        // This is a stub that demonstrates the interface
        this._logger.LogWarning("Decryption is not implemented in RedisKeyVault. Data returned as-is.");
        return Task.FromResult(ciphertext);
    }

    private static string GetSecretKey(string secretName)
    {
        return $"secret:{secretName}";
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
