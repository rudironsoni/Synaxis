// <copyright file="ServiceCollectionExtensions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.Extensions;

using Microsoft.EntityFrameworkCore;
using Synaxis.Shared.Kernel.Domain.Contracts;
using Synaxis.Shared.Kernel.Infrastructure.Configuration;
using Synaxis.Shared.Kernel.Infrastructure.Data;
using Synaxis.Shared.Kernel.Infrastructure.MultiRegion;
using Synaxis.Shared.Kernel.Infrastructure.Repositories;
using Synaxis.Shared.Kernel.Infrastructure.Services;
using Synaxis.Shared.Kernel.Infrastructure.Services.SuperAdmin;
using Synaxis.Providers;

/// <summary>
/// Extension methods for configuring Synaxis.Api services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all Synaxis.Api services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSynaxisApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();

        services.AddSynaxisApiHealthChecks(configuration);
        services.AddSynaxisApiDatabase(configuration);
        services.AddSynaxisApiAuthentication(configuration);
        services.AddSynaxisApiApplicationServices();
        services.AddSynaxisProviders(configuration);
        services.AddSynaxisApiOptions(configuration);
        services.AddCrossRegionHttpClients(configuration);
        services.AddSynaxisApiCors(configuration);

        return services;
    }

    /// <summary>
    /// Adds application-specific services to the container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSynaxisApiApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ISuperAdminService, SuperAdminService>();
        services.AddScoped<ICrossRegionService, CrossRegionService>();
        services.AddScoped<IImpersonationService, ImpersonationService>();
        services.AddScoped<IGlobalAnalyticsService, GlobalAnalyticsService>();
        services.AddScoped<IComplianceService, ComplianceService>();
        services.AddScoped<ISystemHealthService, SystemHealthService>();
        services.AddScoped<IOrganizationLimitService, OrganizationLimitService>();
        services.AddScoped<ISuperAdminAccessValidator, SuperAdminAccessValidator>();

        // Audit infrastructure
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuditArchivalService, AuditArchivalService>();

        // Audit query and export services
        services.AddScoped<Synaxis.Shared.Kernel.Infrastructure.Services.Audit.IAuditQueryService, Synaxis.Shared.Kernel.Infrastructure.Services.Audit.AuditQueryService>();
        services.AddScoped<Synaxis.Shared.Kernel.Infrastructure.Services.Audit.IAuditExportService, Synaxis.Shared.Kernel.Infrastructure.Services.Audit.AuditExportService>();

        // Audit alert services
        services.AddScoped<Synaxis.Shared.Kernel.Infrastructure.EventHandlers.Audit.IAuditAlertService, Synaxis.Shared.Kernel.Infrastructure.EventHandlers.Audit.AuditAlertService>();

        // Audit event handlers
        services.AddScoped<Synaxis.Shared.Kernel.Infrastructure.EventHandlers.Audit.AuditAlertHandler>();
        services.AddScoped<Synaxis.Shared.Kernel.Infrastructure.EventHandlers.Audit.SuspiciousActivityDetectedHandler>();

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IApiKeyService, ApiKeyService>();

        return services;
    }

    /// <summary>
    /// Adds health checks to the container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSynaxisApiHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["ConnectionStrings:DefaultConnection"]
            ?? throw new InvalidOperationException("DefaultConnection is required");

        services.AddHealthChecks()
            .AddNpgSql(
                connectionString: connectionString,
                name: "postgres",
                tags: new[] { "db", "postgres" });

        return services;
    }

    /// <summary>
    /// Adds the database context to the container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSynaxisApiDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["ConnectionStrings:DefaultConnection"]
            ?? throw new InvalidOperationException("DefaultConnection is required");

        services.AddDbContext<SynaxisDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        return services;
    }

    /// <summary>
    /// Adds options configuration to the container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSynaxisApiOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<EmailOptions>(configuration.GetSection("Email"));
        services.Configure<RegionRouterOptions>(configuration.GetSection("RegionRouter"));
        services.Configure<SuperAdminOptions>(configuration.GetSection("SuperAdmin"));

        return services;
    }
}
