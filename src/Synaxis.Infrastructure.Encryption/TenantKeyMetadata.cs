// <copyright file="TenantKeyMetadata.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Encryption;

/// <summary>
/// Metadata for a tenant encryption key.
/// </summary>
/// <param name="KeyId">The key identifier.</param>
/// <param name="Version">The key version.</param>
/// <param name="CreatedAt">The creation timestamp.</param>
/// <param name="IsActive">Whether the key is active.</param>
/// <param name="Algorithm">The encryption algorithm.</param>
public sealed record TenantKeyMetadata(
    string KeyId,
    string Version,
    DateTime CreatedAt,
    bool IsActive,
    string Algorithm)
{
    /// <summary>
    /// Converts the metadata to JSON.
    /// </summary>
    /// <returns>The JSON representation.</returns>
    public string ToJson()
    {
        return System.Text.Json.JsonSerializer.Serialize(this);
    }

    /// <summary>
    /// Creates metadata from JSON.
    /// </summary>
    /// <param name="json">The JSON representation.</param>
    /// <returns>The metadata.</returns>
    public static TenantKeyMetadata FromJson(string json)
    {
        return System.Text.Json.JsonSerializer.Deserialize<TenantKeyMetadata>(json)
            ?? throw new InvalidOperationException("Failed to deserialize key metadata");
    }
}
