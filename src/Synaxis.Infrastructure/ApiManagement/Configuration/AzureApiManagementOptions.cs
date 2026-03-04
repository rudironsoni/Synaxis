// <copyright file="AzureApiManagementOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.ApiManagement.Configuration;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Configuration options for Azure API Management.
/// </summary>
public sealed class AzureApiManagementOptions
{
    /// <summary>
    /// Gets or sets the Azure subscription ID.
    /// </summary>
    [Required(ErrorMessage = "Azure Subscription ID is required")]
    public string SubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API Management service name.
    /// </summary>
    [Required(ErrorMessage = "API Management service name is required")]
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the resource group name.
    /// </summary>
    [Required(ErrorMessage = "Resource group name is required")]
    public string ResourceGroupName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API ID.
    /// </summary>
    public string? ApiId { get; set; }

    /// <summary>
    /// Gets or sets the Azure AD tenant ID for authentication.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the client ID for service principal authentication.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets or sets the client secret for service principal authentication.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets the managed identity client ID (for user-assigned managed identity).
    /// </summary>
    public string? ManagedIdentityClientId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use managed identity for authentication.
    /// </summary>
    public bool UseManagedIdentity { get; set; } = true;

    /// <summary>
    /// Gets or sets the API Management gateway URL.
    /// </summary>
    public string? GatewayUrl { get; set; }

    /// <summary>
    /// Gets or sets the management API URL.
    /// </summary>
    public string? ManagementApiUrl { get; set; } = "https://management.azure.com";

    /// <summary>
    /// Gets or sets the API version for Azure Management API.
    /// </summary>
    public string ApiVersion { get; set; } = "2022-08-01";

    /// <summary>
    /// Gets or sets the request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}
