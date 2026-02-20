// <copyright file="AzureOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Configuration.Options;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Configuration settings for Azure cloud provider.
/// </summary>
public class AzureOptions
{
    /// <summary>
    /// Gets or sets the Azure subscription ID.
    /// </summary>
    [Required(ErrorMessage = "Azure subscription ID is required")]
    public string SubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Azure resource group name.
    /// </summary>
    [Required(ErrorMessage = "Azure resource group is required")]
    public string ResourceGroup { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Azure region/location.
    /// </summary>
    [Required(ErrorMessage = "Azure region is required")]
    public string Region { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Azure tenant ID.
    /// </summary>
    [Required(ErrorMessage = "Azure tenant ID is required")]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Azure client ID for service principal authentication.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Azure client secret for service principal authentication.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to use managed identity.
    /// </summary>
    public bool UseManagedIdentity { get; set; } = true;
}
