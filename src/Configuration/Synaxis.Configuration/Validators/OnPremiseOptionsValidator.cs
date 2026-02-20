// <copyright file="OnPremiseOptionsValidator.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Configuration.Validators;

using System.Net;
using Microsoft.Extensions.Options;
using Synaxis.Configuration.Options;

/// <summary>
/// Validates <see cref="OnPremiseOptions"/> configuration.
/// </summary>
public class OnPremiseOptionsValidator : IValidateOptions<OnPremiseOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, OnPremiseOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ServerAddress))
        {
            failures.Add("OnPremise ServerAddress is required.");
        }
        else if (!IsValidServerAddress(options.ServerAddress))
        {
            failures.Add($"OnPremise ServerAddress '{options.ServerAddress}' is not a valid hostname or IP address.");
        }

        // TLS validation
        if (options.UseTls && string.IsNullOrWhiteSpace(options.CertificatePath))
        {
            failures.Add("OnPremise CertificatePath is required when UseTls is true.");
        }

        if (!string.IsNullOrWhiteSpace(options.CertificatePath))
        {
            // Path format validation - just basic checks
            var invalidChars = Path.GetInvalidPathChars();
            if (options.CertificatePath.Any(c => invalidChars.Contains(c)))
            {
                failures.Add("OnPremise CertificatePath contains invalid characters.");
            }
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }

    private static bool IsValidServerAddress(string address)
    {
        // Check if it's a valid IP address
        if (IPAddress.TryParse(address, out _))
        {
            return true;
        }

        // Basic hostname validation: no spaces, valid characters
        if (address.Contains(' ', StringComparison.Ordinal))
        {
            return false;
        }

        // Check for valid hostname characters
        const string ValidChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.-_";
        if (!address.All(c => ValidChars.Contains(c)))
        {
            return false;
        }

        // Should not start or end with hyphen or dot
        if (address[0] == '-' || address[0] == '.' || address[^1] == '-' || address[^1] == '.')
        {
            return false;
        }

        return true;
    }
}
