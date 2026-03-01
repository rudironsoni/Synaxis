// <copyright file="DependencyInjection.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Application.Services.Audit;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering audit application services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds audit application services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAuditApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuditQueryService, AuditQueryService>();
        services.AddScoped<IAuditExportService, AuditExportService>();

        return services;
    }
}
