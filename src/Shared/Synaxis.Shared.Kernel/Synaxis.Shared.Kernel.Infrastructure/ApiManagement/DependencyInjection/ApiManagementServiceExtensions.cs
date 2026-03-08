// <copyright file="ApiManagementServiceExtensions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Kernel.Infrastructure.ApiManagement.DependencyInjection;

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Synaxis.Shared.Kernel.Infrastructure.ApiManagement.Abstractions;
using Synaxis.Shared.Kernel.Infrastructure.ApiManagement.Configuration;
using Synaxis.Shared.Kernel.Infrastructure.ApiManagement.Services;

/// <summary>
/// Extension methods for registering API Management services.
/// </summary>
public static class ApiManagementServiceExtensions
{
    /// <summary>
    /// Adds API Management services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddApiManagement(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Configure options
        services.Configure<ApiManagementOptions>(
            configuration.GetSection(ApiManagementOptions.SectionName));

        // Register based on provider type
        var options = configuration
            .GetSection(ApiManagementOptions.SectionName)
            .Get<ApiManagementOptions>();

        if (options?.Enabled != true)
        {
            // Register in-memory as default
            services.AddSingleton<IApiManagementService, InMemoryApiManagementService>();
            return services;
        }

        switch (options.Provider)
        {
            case Abstractions.ApiManagementProvider.AzureApiManagement:
                services.AddAzureApiManagement();
                break;

            case Abstractions.ApiManagementProvider.Kong:
                services.AddKongApiManagement();
                break;

            case Abstractions.ApiManagementProvider.InMemory:
            default:
                services.AddSingleton<IApiManagementService, InMemoryApiManagementService>();
                break;
        }

        return services;
    }

    /// <summary>
    /// Adds Azure API Management services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddAzureApiManagement(this IServiceCollection services)
    {
        services.AddHttpClient<IApiManagementService, AzureApiManagementService>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<ApiManagementOptions>>().Value;

            if (options.Azure == null)
            {
                throw new InvalidOperationException("Azure API Management options are not configured");
            }

            client.Timeout = TimeSpan.FromSeconds(options.Azure.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        return services;
    }

    /// <summary>
    /// Adds Kong API Gateway services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddKongApiManagement(this IServiceCollection services)
    {
        services.AddHttpClient<IApiManagementService, KongApiManagementService>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<ApiManagementOptions>>().Value;

            if (options.Kong == null)
            {
                throw new InvalidOperationException("Kong options are not configured");
            }

            var baseUrl = options.Kong.AdminApiUrl;
            if (!baseUrl.EndsWith('/'))
            {
                baseUrl += "/";
            }

            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.Kong.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        return services;
    }

    /// <summary>
    /// Adds in-memory API Management services for testing.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional configuration action.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddInMemoryApiManagement(
        this IServiceCollection services,
        Action<InMemoryOptions>? configureOptions = null)
    {
        if (configureOptions != null)
        {
            services.Configure<ApiManagementOptions>(options =>
            {
                options.Provider = ApiManagementProvider.InMemory;
                options.Enabled = true;
                configureOptions(options.InMemory ??= new InMemoryOptions());
            });
        }

        services.AddSingleton<IApiManagementService, InMemoryApiManagementService>();
        return services;
    }
}
