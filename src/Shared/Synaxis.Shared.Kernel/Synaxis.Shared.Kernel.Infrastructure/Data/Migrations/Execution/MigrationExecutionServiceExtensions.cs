// <copyright file="MigrationExecutionServiceExtensions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Kernel.Infrastructure.Data.Migrations.Execution;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering migration execution services.
/// </summary>
public static class MigrationExecutionServiceExtensions
{
    /// <summary>
    /// Adds migration execution services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMigrationExecutionServices(this IServiceCollection services)
    {
        services.AddSingleton<IMigrationExecutionService, MigrationExecutionService>();
        services.AddSingleton<IPreflightCheckService, PreflightCheckService>();
        services.AddSingleton<IPostDeploymentValidationService, PostDeploymentValidationService>();

        // Register HttpClient for post-deployment validation
        services.AddHttpClient<PostDeploymentValidationService>();

        return services;
    }
}
