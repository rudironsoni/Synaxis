// <copyright file="CloudProviderOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Configuration.Options;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Root configuration for multi-cloud provider settings.
/// </summary>
public class CloudProviderOptions
{
    /// <summary>
    /// Gets or sets the default cloud provider to use.
    /// Valid values: "Azure", "AWS", "GCP", "OnPremise".
    /// </summary>
    [Required(ErrorMessage = "Default provider is required")]
    public string DefaultProvider { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets Azure-specific configuration.
    /// </summary>
    public AzureOptions? Azure { get; set; }

    /// <summary>
    /// Gets or sets AWS-specific configuration.
    /// </summary>
    public AwsOptions? Aws { get; set; }

    /// <summary>
    /// Gets or sets GCP-specific configuration.
    /// </summary>
    public GcpOptions? Gcp { get; set; }

    /// <summary>
    /// Gets or sets On-Premise-specific configuration.
    /// </summary>
    public OnPremiseOptions? OnPremise { get; set; }

    /// <summary>
    /// Gets or sets event store configuration.
    /// </summary>
    public EventStoreOptions? EventStore { get; set; }

    /// <summary>
    /// Gets or sets key vault configuration.
    /// </summary>
    public KeyVaultOptions? KeyVault { get; set; }

    /// <summary>
    /// Gets or sets message bus configuration.
    /// </summary>
    public MessageBusOptions? MessageBus { get; set; }
}
