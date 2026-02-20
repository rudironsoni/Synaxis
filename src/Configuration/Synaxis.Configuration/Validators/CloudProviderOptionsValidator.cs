// <copyright file="CloudProviderOptionsValidator.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Configuration.Validators;

using Microsoft.Extensions.Options;
using Synaxis.Configuration.Options;

/// <summary>
/// Validates <see cref="CloudProviderOptions"/> configuration.
/// </summary>
public class CloudProviderOptionsValidator : IValidateOptions<CloudProviderOptions>
{
    private static readonly string[] ValidProviders = ["Azure", "AWS", "GCP", "OnPremise",];

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, CloudProviderOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.DefaultProvider))
        {
            failures.Add("DefaultProvider is required and cannot be empty.");
        }
        else if (!ValidProviders.Contains(options.DefaultProvider, StringComparer.OrdinalIgnoreCase))
        {
            failures.Add($"DefaultProvider '{options.DefaultProvider}' is not valid. Valid values are: {string.Join(", ", ValidProviders)}.");
        }
        else
        {
            // Check that the selected provider is configured
            var providerName = options.DefaultProvider;
            var providerConfigured = providerName.ToLowerInvariant() switch
            {
                "azure" => options.Azure is not null,
                "aws" => options.Aws is not null,
                "gcp" => options.Gcp is not null,
                "onpremise" => options.OnPremise is not null,
                _ => false,
            };

            if (!providerConfigured)
            {
                failures.Add($"DefaultProvider is set to '{providerName}' but no configuration was found for {providerName}Options.");
            }
        }

        // Check that at least one provider configuration exists when DefaultProvider is set
        if (!string.IsNullOrWhiteSpace(options.DefaultProvider) &&
            options.Azure is null &&
            options.Aws is null &&
            options.Gcp is null &&
            options.OnPremise is null)
        {
            failures.Add("At least one provider configuration (Azure, AWS, GCP, or OnPremise) must be provided.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
