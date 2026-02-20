// <copyright file="GcpOptionsValidator.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Configuration.Validators;

using Microsoft.Extensions.Options;
using Synaxis.Configuration.Options;

/// <summary>
/// Validates <see cref="GcpOptions"/> configuration.
/// </summary>
public class GcpOptionsValidator : IValidateOptions<GcpOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, GcpOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ProjectId))
        {
            failures.Add("GCP ProjectId is required.");
        }
        else if (!IsValidProjectId(options.ProjectId))
        {
            failures.Add("GCP ProjectId must be between 6 and 30 characters, contain only lowercase letters, digits, and hyphens, and must start with a letter.");
        }

        // Validate region format if provided (should match pattern like "us-central1", "europe-west1")
        if (!string.IsNullOrWhiteSpace(options.Region))
        {
            var parts = options.Region.Split('-');
            if (parts.Length < 2 || parts.Length > 3)
            {
                failures.Add($"GCP Region '{options.Region}' does not match expected format (e.g., 'us-central1', 'europe-west1').");
            }
        }

        // If not using default credentials, credentials path is required
        if (!options.UseDefaultCredentials && string.IsNullOrWhiteSpace(options.CredentialsPath))
        {
            failures.Add("GCP CredentialsPath is required when UseDefaultCredentials is false.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }

    private static bool IsValidProjectId(string projectId)
    {
        // GCP project ID rules:
        // - 6 to 30 characters
        // - Lowercase letters, digits, and hyphens
        // - Must start with a letter
        // - Cannot end with a hyphen
        if (projectId.Length < 6 || projectId.Length > 30)
        {
            return false;
        }

        if (!char.IsLower(projectId[0]))
        {
            return false;
        }

        if (projectId[^1] == '-')
        {
            return false;
        }

        return projectId.All(c => char.IsLower(c) || char.IsDigit(c) || c == '-');
    }
}
