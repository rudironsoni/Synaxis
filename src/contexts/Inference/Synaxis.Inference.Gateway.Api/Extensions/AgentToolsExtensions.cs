// <copyright file="AgentToolsExtensions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Extensions;

using Synaxis.InferenceGateway.Infrastructure.Agents.Tools;

/// <summary>
/// Extension methods for registering agent tools services.
/// </summary>
public static class AgentToolsExtensions
{
    /// <summary>
    /// Adds all agent tool services to the container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAgentTools(this IServiceCollection services)
    {
        services.AddScoped<IProviderTool, ProviderTool>();
        services.AddScoped<IAlertTool, AlertTool>();
        services.AddScoped<IRoutingTool, RoutingTool>();
        services.AddScoped<IHealthTool, HealthTool>();
        services.AddScoped<IAuditTool, AuditTool>();
        services.AddScoped<IAgentTools, AgentTools>();

        return services;
    }
}
