// <copyright file="ServiceCollectionExtensions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.DependencyInjection
{
    using System;
    using Mediator;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Synaxis.Abstractions.Execution;
    using Synaxis.Abstractions.Routing;
    using Synaxis.Behaviors;
    using Synaxis.Commands.Chat;
    using Synaxis.Commands.Embeddings;
    using Synaxis.Contracts.V1.Messages;
    using Synaxis.Execution;
    using Synaxis.Handlers.Chat;
    using Synaxis.Handlers.Embeddings;
    using Synaxis.Routing;

    /// <summary>
    /// Extension methods for configuring Synaxis services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Synaxis services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddSynaxis(
            this IServiceCollection services,
            Action<SynaxisOptions>? configure = null)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Configure options
            if (configure is not null)
            {
                services.Configure(configure);
            }
            else
            {
                services.Configure<SynaxisOptions>(_ => { });
            }

            // Register Mediator
            services.AddMediator(options =>
            {
                options.ServiceLifetime = ServiceLifetime.Scoped;
            });

            // Register handlers
            services.TryAddScoped<ChatCommandHandler>();
            services.TryAddScoped<ChatStreamHandler>();
            services.TryAddScoped<EmbeddingCommandHandler>();

            // Register pipeline behaviors
            services.TryAddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            services.TryAddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            services.TryAddScoped(typeof(IPipelineBehavior<,>), typeof(MetricsBehavior<,>));
            services.TryAddScoped(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));

            // Register executors
            services.TryAddScoped<ICommandExecutor<ChatCommand, ChatResponse>, MediatorCommandExecutor<ChatCommand, ChatResponse>>();
            services.TryAddScoped<ICommandExecutor<EmbeddingCommand, EmbeddingResponse>, MediatorCommandExecutor<EmbeddingCommand, EmbeddingResponse>>();
            services.TryAddScoped<IStreamExecutor<ChatStreamCommand, ChatStreamChunk>, MediatorStreamExecutor<ChatStreamCommand, ChatStreamChunk>>();

            // Register routing
            services.TryAddScoped<IProviderSelector, ProviderSelector>();
            services.TryAddSingleton<IRoutingStrategy, RoundRobinRoutingStrategy>();
            services.TryAddSingleton<IRoutingStrategy, LeastLoadedRoutingStrategy>();
            services.TryAddSingleton<IRoutingStrategy, PriorityRoutingStrategy>();

            return services;
        }
    }

    /// <summary>
    /// Configuration options for Synaxis.
    /// </summary>
    public sealed class SynaxisOptions
    {
        /// <summary>
        /// Gets or sets the default routing strategy name.
        /// </summary>
        public string DefaultRoutingStrategy { get; set; } = "RoundRobin";

        /// <summary>
        /// Gets or sets a value indicating whether to enable pipeline behaviors.
        /// </summary>
        public bool EnablePipelineBehaviors { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to enable request validation.
        /// </summary>
        public bool EnableValidation { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to enable metrics collection.
        /// </summary>
        public bool EnableMetrics { get; set; } = true;
    }
}
