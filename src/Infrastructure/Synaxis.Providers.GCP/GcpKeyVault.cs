// <copyright file="GcpKeyVault.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.GCP;

using System;
using System.Threading;
using System.Threading.Tasks;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// Stub implementation for future GCP KeyVault integration using Cloud KMS and Secret Manager.
/// </summary>
public class GcpKeyVault : IKeyVault
{
    /// <inheritdoc />
    public Task SetSecretAsync(
        string secretName,
        string secretValue,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("GCP KeyVault integration is not yet implemented. This stub will use Secret Manager for secret storage.");
    }

    /// <inheritdoc />
    public Task<string?> GetSecretAsync(
        string secretName,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("GCP KeyVault integration is not yet implemented. This stub will use Secret Manager for secret storage.");
    }

    /// <inheritdoc />
    public Task DeleteSecretAsync(
        string secretName,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("GCP KeyVault integration is not yet implemented. This stub will use Secret Manager for secret storage.");
    }

    /// <inheritdoc />
    public Task<byte[]> EncryptAsync(
        string keyName,
        byte[] plaintext,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("GCP KeyVault integration is not yet implemented. This stub will use Cloud KMS for encryption operations.");
    }

    /// <inheritdoc />
    public Task<byte[]> DecryptAsync(
        string keyName,
        byte[] ciphertext,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("GCP KeyVault integration is not yet implemented. This stub will use Cloud KMS for decryption operations.");
    }
}
