// <copyright file="AwsKeyVault.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Providers.AWS;

using System;
using System.Threading;
using System.Threading.Tasks;
using Synaxis.Abstractions.Cloud;

/// <summary>
/// Stub implementation for future AWS KeyVault integration using AWS KMS and Secrets Manager.
/// </summary>
public class AwsKeyVault : IKeyVault
{
    /// <inheritdoc />
    public Task SetSecretAsync(
        string secretName,
        string secretValue,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("AWS KeyVault integration is not yet implemented. This stub will use AWS Secrets Manager for secret storage.");
    }

    /// <inheritdoc />
    public Task<string?> GetSecretAsync(
        string secretName,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("AWS KeyVault integration is not yet implemented. This stub will use AWS Secrets Manager for secret storage.");
    }

    /// <inheritdoc />
    public Task DeleteSecretAsync(
        string secretName,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("AWS KeyVault integration is not yet implemented. This stub will use AWS Secrets Manager for secret storage.");
    }

    /// <inheritdoc />
    public Task<byte[]> EncryptAsync(
        string keyName,
        byte[] plaintext,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("AWS KeyVault integration is not yet implemented. This stub will use AWS KMS for encryption operations.");
    }

    /// <inheritdoc />
    public Task<byte[]> DecryptAsync(
        string keyName,
        byte[] ciphertext,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("AWS KeyVault integration is not yet implemented. This stub will use AWS KMS for decryption operations.");
    }
}
