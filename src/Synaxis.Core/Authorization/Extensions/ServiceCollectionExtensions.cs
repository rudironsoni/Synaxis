// <copyright file="ServiceCollectionExtensions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Authorization;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering authorization services in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all authorization handlers to the specified <see cref="IServiceCollection"/>.
    /// Note: Call AddAuthorization() separately in your API project to configure policies.
    /// </summary>
    /// <param name="services">The service collection to add authorization handlers to.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddAuthorizationHandlers(this IServiceCollection services)
    {
        // Register all authorization handlers
        services.AddScoped<IAuthorizationHandler, OrgAdminAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, TeamAdminAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, MemberAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, ViewerAuthorizationHandler>();

        return services;
    }
}
