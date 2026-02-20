// <copyright file="AwsOptionsValidator.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Configuration.Validators;

using Microsoft.Extensions.Options;
using Synaxis.Configuration.Options;

/// <summary>
/// Validates <see cref="AwsOptions"/> configuration.
/// </summary>
public class AwsOptionsValidator : IValidateOptions<AwsOptions>
{
    private static readonly string[] ValidRegions =
    [
        "us-east-1", "us-east-2", "us-west-1", "us-west-2",
        "eu-west-1", "eu-west-2", "eu-west-3", "eu-central-1", "eu-north-1",
        "ap-southeast-1", "ap-southeast-2", "ap-northeast-1", "ap-northeast-2",
        "ap-south-1", "ca-central-1", "sa-east-1",
    ];

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, AwsOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Region))
        {
            failures.Add("AWS Region is required.");
        }
        else if (!ValidRegions.Contains(options.Region, StringComparer.OrdinalIgnoreCase))
        {
            failures.Add($"AWS Region '{options.Region}' is not a valid AWS region code.");
        }

        // If not using default credentials, access keys are required
        if (!options.UseDefaultCredentials)
        {
            if (string.IsNullOrWhiteSpace(options.AccessKeyId))
            {
                failures.Add("AWS AccessKeyId is required when UseDefaultCredentials is false.");
            }

            if (string.IsNullOrWhiteSpace(options.SecretAccessKey))
            {
                failures.Add("AWS SecretAccessKey is required when UseDefaultCredentials is false.");
            }
        }

        // If session token is provided, access keys should also be provided
        if (!string.IsNullOrWhiteSpace(options.SessionToken) &&
            (string.IsNullOrWhiteSpace(options.AccessKeyId) || string.IsNullOrWhiteSpace(options.SecretAccessKey)))
        {
            failures.Add("AWS AccessKeyId and SecretAccessKey are required when SessionToken is provided.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
