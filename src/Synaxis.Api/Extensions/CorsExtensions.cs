// <copyright file="CorsExtensions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.Extensions;

/// <summary>
/// Extension methods for configuring CORS policies.
/// </summary>
public static class CorsExtensions
{
    /// <summary>
    /// Adds CORS configuration to the container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSynaxisApiCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                var allowedOrigins = configuration["Synaxis:Cors:AllowedOrigins"]?.Split(',')
                    ?? new[] { "http://localhost:8080" };
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        return services;
    }
}
