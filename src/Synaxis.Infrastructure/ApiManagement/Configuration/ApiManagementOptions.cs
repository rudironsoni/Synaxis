// <copyright file="ApiManagementOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.ApiManagement.Configuration;

using System.ComponentModel.DataAnnotations;
using Synaxis.Infrastructure.ApiManagement.Abstractions;

/// <summary>
/// Configuration options for API Management integration.
/// </summary>
public sealed class ApiManagementOptions
{
    /// <summary>
    /// Gets or sets the configuration section name.
    /// </summary>
    public const string SectionName = "ApiManagement";

    /// <summary>
    /// Gets or sets the API Management provider type.
    /// </summary>
    [Required(ErrorMessage = "API Management provider is required")]
    public ApiManagementProvider Provider { get; set; } = ApiManagementProvider.InMemory;

    /// <summary>
    /// Gets or sets a value indicating whether API Management integration is enabled.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the API key header name.
    /// </summary>
    public string ApiKeyHeaderName { get; set; } = "X-API-Key";

    /// <summary>
    /// Gets or sets the Azure API Management configuration.
    /// </summary>
    public AzureApiManagementOptions? Azure { get; set; }

    /// <summary>
    /// Gets or sets the Kong API Gateway configuration.
    /// </summary>
    public KongOptions? Kong { get; set; }

    /// <summary>
    /// Gets or sets the in-memory configuration for development/testing.
    /// </summary>
    public InMemoryOptions? InMemory { get; set; }

    /// <summary>
    /// Gets or sets the default rate limit configuration.
    /// </summary>
    public RateLimitOptions RateLimit { get; set; } = new();
}
