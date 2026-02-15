// <copyright file="GcpOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Extensions.DI;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Configuration settings for Google Cloud Platform provider.
/// </summary>
public class GcpOptions
{
    /// <summary>
    /// Gets or sets the GCP project ID.
    /// </summary>
    [Required(ErrorMessage = "GCP project ID is required")]
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the GCP region/zone.
    /// </summary>
    [Required(ErrorMessage = "GCP region is required")]
    public string Region { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path to the service account JSON credentials file.
    /// </summary>
    public string CredentialsPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the service account email for impersonation.
    /// </summary>
    public string ServiceAccountEmail { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to use Application Default Credentials.
    /// </summary>
    public bool UseDefaultCredentials { get; set; } = true;
}
