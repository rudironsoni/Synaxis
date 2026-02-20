// <copyright file="KeyVaultOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Configuration.Options;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Configuration settings for key vault implementation.
/// </summary>
public class KeyVaultOptions
{
    /// <summary>
    /// Gets or sets the key vault provider type (e.g., "AzureKeyVault", "AWSKMS", "GCPKMS").
    /// </summary>
    [Required(ErrorMessage = "Key vault provider is required")]
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the key vault URI.
    /// </summary>
    public string VaultUri { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default key name for encryption operations.
    /// </summary>
    public string DefaultKeyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the key version to use (empty for latest).
    /// </summary>
    public string KeyVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to cache secrets locally.
    /// </summary>
    public bool EnableCache { get; set; } = true;

    /// <summary>
    /// Gets or sets the cache duration in seconds.
    /// </summary>
    [Range(60, 86400, ErrorMessage = "Cache duration must be between 60 and 86400 seconds")]
    public int CacheDurationSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets a value indicating whether to use managed identity for authentication.
    /// </summary>
    public bool UseManagedIdentity { get; set; } = true;
}
