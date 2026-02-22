// <copyright file="SignalRAdapterServiceCollectionExtensions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Adapters.SignalR.DependencyInjection
{
    using System;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.Extensions.DependencyInjection;
    using Synaxis.Adapters.SignalR.Connection;
    using Synaxis.Adapters.SignalR.Groups;
    using Synaxis.Adapters.SignalR.Hubs;

    /// <summary>
    /// Extension methods for configuring SignalR adapter services.
    /// </summary>
    public static class SignalRAdapterServiceCollectionExtensions
    {
        /// <summary>
        /// Adds SignalR adapter services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action for SignalR adapter options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddSynaxisAdapterSignalR(
            this IServiceCollection services,
            Action<SignalRAdapterOptions>? configure = null)
        {
            ArgumentNullException.ThrowIfNull(services);
            // Register options
            var options = new SignalRAdapterOptions();
            configure?.Invoke(options);
            services.AddSingleton(options);

            // Register managers
            services.AddSingleton<ConnectionManager>();
            services.AddSingleton<GroupManager>();

            // Register SignalR
            services.AddSignalR(hubOptions =>
            {
                hubOptions.EnableDetailedErrors = options.EnableDetailedErrors;
                hubOptions.MaximumReceiveMessageSize = options.MaximumReceiveMessageSize;
            });

            return services;
        }

        /// <summary>
        /// Maps SignalR adapter hubs to the endpoint route builder.
        /// </summary>
        /// <param name="endpoints">The endpoint route builder.</param>
        /// <param name="options">Optional SignalR adapter options. If not provided, uses default options.</param>
        /// <returns>The endpoint route builder for chaining.</returns>
        public static IEndpointRouteBuilder MapSynaxisAdapterSignalR(
            this IEndpointRouteBuilder endpoints,
            SignalRAdapterOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(endpoints);
            options ??= new SignalRAdapterOptions();

            // Map hubs
            endpoints.MapHub<ChatHub>($"{options.Path}/chat");
            endpoints.MapHub<SynaxisHub>(options.Path);

            return endpoints;
        }
    }
}
