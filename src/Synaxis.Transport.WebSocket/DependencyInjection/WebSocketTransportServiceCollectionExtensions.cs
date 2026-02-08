// <copyright file="WebSocketTransportServiceCollectionExtensions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Transport.WebSocket.DependencyInjection
{
    using System;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Synaxis.Transport.WebSocket.Handlers;
    using Synaxis.Transport.WebSocket.Middleware;

    /// <summary>
    /// Extension methods for configuring WebSocket transport services.
    /// </summary>
    public static class WebSocketTransportServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Synaxis WebSocket transport services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
        public static IServiceCollection AddSynaxisTransportWebSocket(
            this IServiceCollection services,
            Action<WebSocketTransportOptions>? configure = null)
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
                services.Configure<WebSocketTransportOptions>(_ => { });
            }

            // Register handler
            services.TryAddScoped<WebSocketHandler>();

            return services;
        }

        /// <summary>
        /// Adds Synaxis WebSocket transport middleware to the application pipeline.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when app is null.</exception>
        public static IApplicationBuilder UseSynaxisTransportWebSocket(this IApplicationBuilder app)
        {
            if (app is null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            // Enable WebSocket support
            var webSocketOptions = new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromSeconds(30),
            };

            app.UseWebSockets(webSocketOptions);

            // Add WebSocket middleware
            app.UseMiddleware<WebSocketMiddleware>();

            return app;
        }
    }
}
