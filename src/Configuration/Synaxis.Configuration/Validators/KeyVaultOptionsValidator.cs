// <copyright file="KeyVaultOptionsValidator.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Configuration.Validators;

using Microsoft.Extensions.Options;
using Synaxis.Configuration.Options;

/// <summary>
/// Validates <see cref="KeyVaultOptions"/> configuration.
/// </summary>
public class KeyVaultOptionsValidator : IValidateOptions<KeyVaultOptions>
{
    private static readonly string[] ValidProviders = ["AzureKeyVault", "AWSKMS", "GCPKMS", "HashiCorpVault"];

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, KeyVaultOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Provider))
        {
            failures.Add("KeyVault Provider is required.");
        }
        else if (!ValidProviders.Contains(options.Provider, StringComparer.OrdinalIgnoreCase))
        {
            failures.Add($"KeyVault Provider '{options.Provider}' is not valid. Valid values are: {string.Join(", ", ValidProviders)}.");
        }

        if (!string.IsNullOrWhiteSpace(options.VaultUri) && !IsValidUri(options.VaultUri))
        {
            failures.Add($"KeyVault VaultUri '{options.VaultUri}' is not a valid URI.");
        }

        // Validate cache duration
        if (options.CacheDurationSeconds < 60 || options.CacheDurationSeconds > 86400)
        {
            failures.Add("KeyVault CacheDurationSeconds must be between 60 and 86400 seconds.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }

    private static bool IsValidUri(string uriString)
    {
        return Uri.TryCreate(uriString, UriKind.Absolute, out _);
    }
}
