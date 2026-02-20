// <copyright file="AzureOptionsValidator.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Configuration.Validators;

using System.Globalization;
using Microsoft.Extensions.Options;
using Synaxis.Configuration.Options;

/// <summary>
/// Validates <see cref="AzureOptions"/> configuration.
/// </summary>
public class AzureOptionsValidator : IValidateOptions<AzureOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, AzureOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.TenantId))
        {
            failures.Add("Azure TenantId is required.");
        }
        else if (!IsValidGuid(options.TenantId))
        {
            failures.Add("Azure TenantId must be a valid GUID.");
        }

        if (string.IsNullOrWhiteSpace(options.SubscriptionId))
        {
            failures.Add("Azure SubscriptionId is required.");
        }
        else if (!IsValidGuid(options.SubscriptionId))
        {
            failures.Add("Azure SubscriptionId must be a valid GUID.");
        }

        if (string.IsNullOrWhiteSpace(options.ResourceGroup))
        {
            failures.Add("Azure ResourceGroup is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Region))
        {
            failures.Add("Azure Region is required.");
        }

        // If not using managed identity, client credentials are required
        if (!options.UseManagedIdentity)
        {
            if (string.IsNullOrWhiteSpace(options.ClientId))
            {
                failures.Add("Azure ClientId is required when UseManagedIdentity is false.");
            }
            else if (!IsValidGuid(options.ClientId))
            {
                failures.Add("Azure ClientId must be a valid GUID.");
            }

            if (string.IsNullOrWhiteSpace(options.ClientSecret))
            {
                failures.Add("Azure ClientSecret is required when UseManagedIdentity is false.");
            }
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }

    private static bool IsValidGuid(string value)
    {
        return Guid.TryParseExact(value, "D", out _) ||
               Guid.TryParse(value, out _);
    }
}
