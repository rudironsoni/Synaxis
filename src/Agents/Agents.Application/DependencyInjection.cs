// <copyright file="DependencyInjection.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application;

using Microsoft.Extensions.DependencyInjection;
using Synaxis.Agents.Application.Interfaces;
using Synaxis.Agents.Application.Services;

/// <summary>
/// Extension methods for registering Agents application services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Agents application services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAgentsApplication(this IServiceCollection services)
    {
        // Register application services using explicit type aliases to avoid ambiguity
        services.AddScoped<IAgentConfigurationService, AgentConfigurationService>();
        services.AddScoped<Services.IAgentExecutionService, AgentExecutionService>();
        services.AddScoped<IAgentWorkflowService, AgentWorkflowService>();

        return services;
    }
}
