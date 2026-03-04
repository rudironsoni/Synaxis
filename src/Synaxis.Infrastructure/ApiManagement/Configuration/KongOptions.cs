// <copyright file="KongOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.ApiManagement.Configuration;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Configuration options for Kong API Gateway.
/// </summary>
public sealed class KongOptions
{
    /// <summary>
    /// Gets or sets the Kong Admin API base URL.
    /// </summary>
    [Required(ErrorMessage = "Kong Admin API URL is required")]
    [Url(ErrorMessage = "Invalid URL format")]
    public string AdminApiUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API key for Kong Admin API authentication.
    /// </summary>
    public string? AdminApiKey { get; set; }

    /// <summary>
    /// Gets or sets the username for basic authentication.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the password for basic authentication.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets a value indicating whether to use HTTPS.
    /// </summary>
    public bool UseHttps { get; set; } = true;

    /// <summary>
    /// Gets or sets the certificate thumbprint for client certificate authentication.
    /// </summary>
    public string? ClientCertificateThumbprint { get; set; }

    /// <summary>
    /// Gets or sets the consumer ID or username pattern for key provisioning.
    /// </summary>
    public string? ConsumerPattern { get; set; }
}
