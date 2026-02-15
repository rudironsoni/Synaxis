// <copyright file="EncryptionOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Encryption;

using Microsoft.Extensions.Options;

/// <summary>
/// Configuration options for the encryption service.
/// </summary>
public sealed class EncryptionOptions
{
    /// <summary>
    /// Gets or sets the default key size in bits for data encryption keys.
    /// </summary>
    /// <remarks>Default is 256 bits (AES-256).</remarks>
    public int DefaultKeySizeBits { get; set; } = 256;

    /// <summary>
    /// Gets or sets the default key rotation interval in days.
    /// </summary>
    /// <remarks>Default is 90 days.</remarks>
    public int DefaultKeyRotationDays { get; set; } = 90;

    /// <summary>
    /// Gets or sets the maximum number of key versions to retain.
    /// </summary>
    /// <remarks>Default is 5 versions.</remarks>
    public int MaxKeyVersions { get; set; } = 5;

    /// <summary>
    /// Gets or sets the encryption algorithm to use.
    /// </summary>
    /// <remarks>Default is AES-256-GCM.</remarks>
    public string DefaultAlgorithm { get; set; } = "AES-256-GCM";

    /// <summary>
    /// Validates the encryption options.
    /// </summary>
    /// <exception cref="OptionsValidationException">Thrown when options are invalid.</exception>
    public void Validate()
    {
        if (this.DefaultKeySizeBits is not 128 and not 192 and not 256)
        {
            throw new OptionsValidationException(
                nameof(this.DefaultKeySizeBits),
                typeof(EncryptionOptions),
                ["Key size must be 128, 192, or 256 bits"]);
        }

        if (this.DefaultKeyRotationDays <= 0)
        {
            throw new OptionsValidationException(
                nameof(this.DefaultKeyRotationDays),
                typeof(EncryptionOptions),
                ["Key rotation interval must be greater than 0"]);
        }

        if (this.MaxKeyVersions <= 0)
        {
            throw new OptionsValidationException(
                nameof(this.MaxKeyVersions),
                typeof(EncryptionOptions),
                ["Max key versions must be greater than 0"]);
        }

        if (string.IsNullOrWhiteSpace(this.DefaultAlgorithm))
        {
            throw new OptionsValidationException(
                nameof(this.DefaultAlgorithm),
                typeof(EncryptionOptions),
                ["Default algorithm cannot be empty"]);
        }
    }
}
