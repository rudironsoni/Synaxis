// <copyright file="DestinationConfig.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Gateway.Api.Configuration;

/// <summary>
/// Represents a destination configuration.
/// </summary>
public class DestinationConfig
{
    /// <summary>
    /// Gets or sets the destination identifier.
    /// </summary>
    public string DestinationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the address.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the health check address.
    /// </summary>
    public string? HealthCheckAddress { get; set; }

    /// <summary>
    /// Gets or sets the metadata.
    /// </summary>
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the weight for weighted load balancing.
    /// </summary>
    public int Weight { get; set; } = 1;
}
