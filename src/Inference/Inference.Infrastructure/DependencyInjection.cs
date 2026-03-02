// <copyright file="DependencyInjection.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Synaxis.Inference.Infrastructure.Persistence;

/// <summary>
/// Extension methods for registering inference infrastructure services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds inference infrastructure services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddInferenceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        AddPersistence(services, configuration);

        return services;
    }

    private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("InferenceCosmosDb");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<InferenceDbContext>(options =>
                options.UseCosmos(connectionString, "InferenceDatabase"));
        }
        else
        {
            // Fallback to SQL Server for development
            services.AddDbContext<InferenceDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly("Inference.Infrastructure")));
        }

        services.AddScoped<IInferenceRepository, InferenceRepository>();
    }
}
