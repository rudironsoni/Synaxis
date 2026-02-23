// <copyright file="HttpClientExtensions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.Extensions;

/// <summary>
/// Extension methods for configuring cross-region HTTP clients.
/// </summary>
public static class HttpClientExtensions
{
    /// <summary>
    /// Adds HTTP clients for cross-region communication.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCrossRegionHttpClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var euWest1Endpoint = configuration["Regions:EuWest1:Endpoint"];
        if (!string.IsNullOrEmpty(euWest1Endpoint))
        {
            services.AddHttpClient("eu-west-1", client =>
            {
                client.BaseAddress = new Uri(euWest1Endpoint);
                client.Timeout = TimeSpan.FromSeconds(30);
            });
        }

        var usEast1Endpoint = configuration["Regions:UsEast1:Endpoint"];
        if (!string.IsNullOrEmpty(usEast1Endpoint))
        {
            services.AddHttpClient("us-east-1", client =>
            {
                client.BaseAddress = new Uri(usEast1Endpoint);
                client.Timeout = TimeSpan.FromSeconds(30);
            });
        }

        var saEast1Endpoint = configuration["Regions:SaEast1:Endpoint"];
        if (!string.IsNullOrEmpty(saEast1Endpoint))
        {
            services.AddHttpClient("sa-east-1", client =>
            {
                client.BaseAddress = new Uri(saEast1Endpoint);
                client.Timeout = TimeSpan.FromSeconds(30);
            });
        }

        return services;
    }
}
