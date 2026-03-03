// <copyright file="GatewayRoutingConfig.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Gateway.Api.Configuration;

/// <summary>
/// Configuration for the API Gateway routing.
/// </summary>
public class GatewayRoutingConfig
{
    /// <summary>
    /// Gets or sets the configuration section name.
    /// </summary>
    public const string SectionName = "GatewayRouting";

    /// <summary>
    /// Gets or sets the routes.
    /// </summary>
    public IList<GatewayRoute> Routes { get; set; } = new List<GatewayRoute>();

    /// <summary>
    /// Gets or sets the clusters.
    /// </summary>
    public IList<GatewayCluster> Clusters { get; set; } = new List<GatewayCluster>();

    /// <summary>
    /// Gets or sets the global rate limiting options.
    /// </summary>
    public RateLimitOptions RateLimiting { get; set; } = new();

    /// <summary>
    /// Gets or sets the authentication options.
    /// </summary>
    public AuthOptions Authentication { get; set; } = new();
}
