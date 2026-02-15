// <copyright file="AwsOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Extensions.DI;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Configuration settings for AWS cloud provider.
/// </summary>
public class AwsOptions
{
    /// <summary>
    /// Gets or sets the AWS access key ID.
    /// </summary>
    public string AccessKeyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the AWS secret access key.
    /// </summary>
    public string SecretAccessKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the AWS region.
    /// </summary>
    [Required(ErrorMessage = "AWS region is required")]
    public string Region { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the AWS account ID.
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the AWS session token for temporary credentials.
    /// </summary>
    public string SessionToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the AWS profile name for credential resolution.
    /// </summary>
    public string Profile { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to use the default credential provider chain.
    /// </summary>
    public bool UseDefaultCredentials { get; set; } = true;
}
