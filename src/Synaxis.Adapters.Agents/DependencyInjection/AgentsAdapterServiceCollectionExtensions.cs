// <copyright file="AgentsAdapterServiceCollectionExtensions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Adapters.Agents.DependencyInjection
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Synaxis.Adapters.Agents.Handlers;
    using Synaxis.Adapters.Agents.State;
    using Synaxis.Adapters.Agents.Tools;

    /// <summary>
    /// Extension methods for configuring Synaxis Agents adapter services.
    /// </summary>
    public static class AgentsAdapterServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Synaxis Agents adapter services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="configureOptions">An optional action to configure the adapter options.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddSynaxisAdapterAgents(
            this IServiceCollection services,
            Action<AgentsAdapterOptions>? configureOptions = null)
        {
            ArgumentNullException.ThrowIfNull(services);
            // Register options
            var optionsBuilder = services.AddOptions<AgentsAdapterOptions>();
            if (configureOptions != null)
            {
                optionsBuilder.Configure(configureOptions);
            }

            // Register default in-memory storage
            services.AddSingleton<IConversationStorage, MemoryConversationStorage>();

            // Register Synaxis Agents adapter components
            services.AddSingleton<ConversationStateManager>(sp =>
            {
                var storage = sp.GetRequiredService<IConversationStorage>();
                var options = sp.GetRequiredService<IOptions<AgentsAdapterOptions>>().Value;
                return new ConversationStateManager(storage, options.MaxHistoryMessages);
            });

            services.AddTransient<MediatorActivityHandler>();
            services.AddTransient<ChatTool>();
            services.AddTransient<RoutingTool>();

            return services;
        }

        /// <summary>
        /// Adds Synaxis Agents adapter services with a custom storage provider.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        /// <param name="storageFactory">A factory function to create the storage provider.</param>
        /// <param name="configureOptions">An optional action to configure the adapter options.</param>
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddSynaxisAdapterAgents(
            this IServiceCollection services,
            Func<IServiceProvider, IConversationStorage> storageFactory,
            Action<AgentsAdapterOptions>? configureOptions = null)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(storageFactory);
            // Register options
            var optionsBuilder = services.AddOptions<AgentsAdapterOptions>();
            if (configureOptions != null)
            {
                optionsBuilder.Configure(configureOptions);
            }

            // Register custom storage
            services.AddSingleton(storageFactory);

            // Register Synaxis Agents adapter components
            services.AddSingleton<ConversationStateManager>(sp =>
            {
                var storage = sp.GetRequiredService<IConversationStorage>();
                var options = sp.GetRequiredService<IOptions<AgentsAdapterOptions>>().Value;
                return new ConversationStateManager(storage, options.MaxHistoryMessages);
            });

            services.AddTransient<MediatorActivityHandler>();
            services.AddTransient<ChatTool>();
            services.AddTransient<RoutingTool>();

            return services;
        }
    }
}
