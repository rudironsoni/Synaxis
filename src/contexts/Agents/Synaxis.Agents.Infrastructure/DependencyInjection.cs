// <copyright file="DependencyInjection.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Infrastructure;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering Agents infrastructure services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Agents infrastructure services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAgentsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register repositories - use explicit namespace to avoid ambiguity
        services.AddScoped<Application.Interfaces.IAgentConfigurationRepository, Repositories.AgentConfigurationRepository>();
        services.AddScoped<Application.Interfaces.IAgentExecutionRepository, Repositories.AgentExecutionRepository>();
        services.AddScoped<Application.Interfaces.IAgentWorkflowRepository, Repositories.AgentWorkflowRepository>();

        return services;
    }
}
