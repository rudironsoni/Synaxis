// <copyright file="McpAdapterServiceCollectionExtensions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Adapters.Mcp.DependencyInjection
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Options;
    using Synaxis.Adapters.Mcp.Prompts;
    using Synaxis.Adapters.Mcp.Resources;
    using Synaxis.Adapters.Mcp.Server;
    using Synaxis.Adapters.Mcp.Transports;

    /// <summary>
    /// Extension methods for configuring MCP adapter services.
    /// </summary>
    public static class McpAdapterServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Synaxis MCP adapter services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddSynaxisAdapterMcp(
            this IServiceCollection services,
            Action<McpAdapterOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(services);
            // Configure options
            if (configure is not null)
            {
                services.Configure(configure);
            }
            else
            {
                services.Configure<McpAdapterOptions>(_ => { });
            }

            // Register core MCP services
            services.TryAddSingleton<IToolRegistry, ToolRegistry>();
            services.TryAddScoped<SynaxisMcpServer>();

            // Register transport factories
            services.TryAddScoped<IMcpTransport>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<McpAdapterOptions>>().Value;
                var server = sp.GetRequiredService<SynaxisMcpServer>();

                return options.DefaultTransport switch
                {
                    McpTransportType.Stdio => ActivatorUtilities.CreateInstance<StdioTransport>(sp, server),
                    McpTransportType.Http => ActivatorUtilities.CreateInstance<HttpTransport>(sp, server, options.HttpBaseUrl),
                    McpTransportType.Sse => ActivatorUtilities.CreateInstance<SseTransport>(sp, server, options.SseEndpoint),
                    _ => throw new InvalidOperationException($"Unknown transport type: {options.DefaultTransport}")
                };
            });

            return services;
        }

        /// <summary>
        /// Adds model resource provider with the specified models.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureModels">Action to configure available models.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMcpModelResources(
            this IServiceCollection services,
            Action<ModelResourceBuilder> configureModels)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configureModels);
            var builder = new ModelResourceBuilder();
            configureModels(builder);

            services.TryAddSingleton(new ModelResourceProvider(builder.Build()));

            return services;
        }

        /// <summary>
        /// Adds system prompt provider with the specified prompts.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurePrompts">Action to configure available prompts.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMcpSystemPrompts(
            this IServiceCollection services,
            Action<PromptTemplateBuilder> configurePrompts)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configurePrompts);
            var builder = new PromptTemplateBuilder();
            configurePrompts(builder);

            services.TryAddSingleton(new SystemPromptProvider(builder.Build()));

            return services;
        }
    }
}
