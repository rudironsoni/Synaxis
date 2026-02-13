// <copyright file="HttpTransportServiceCollectionExtensions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Transport.Http.DependencyInjection
{
    using System;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.Extensions.DependencyInjection;
    using Synaxis.Transport.Http.Filters;
    using Synaxis.Transport.Http.Middleware;

    /// <summary>
    /// Extension methods for configuring Synaxis HTTP transport services.
    /// </summary>
    public static class HttpTransportServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Synaxis HTTP transport services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddSynaxisTransportHttp(this IServiceCollection services)
        {
            return services.AddSynaxisTransportHttp(_ => { });
        }

        /// <summary>
        /// Adds Synaxis HTTP transport services to the service collection with configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Action to configure transport options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddSynaxisTransportHttp(
            this IServiceCollection services,
            Action<HttpTransportOptions> configureOptions)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            // Configure options
            services.Configure(configureOptions);

            // Add controllers from this assembly
            services.AddControllers(options =>
            {
                options.Filters.Add<SynaxisExceptionFilter>();
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            })
            .AddApplicationPart(typeof(HttpTransportServiceCollectionExtensions).Assembly);

            return services;
        }

        /// <summary>
        /// Adds Synaxis HTTP transport middleware to the application pipeline.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <returns>The application builder for chaining.</returns>
        public static IApplicationBuilder UseSynaxisTransportHttp(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            app.UseMiddleware<CorrelationIdMiddleware>();

            return app;
        }

        /// <summary>
        /// Maps Synaxis HTTP transport endpoints to the endpoint route builder.
        /// </summary>
        /// <param name="endpoints">The endpoint route builder.</param>
        /// <returns>The endpoint route builder for chaining.</returns>
        public static IEndpointRouteBuilder MapSynaxisTransportHttp(this IEndpointRouteBuilder endpoints)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            endpoints.MapControllers();

            return endpoints;
        }
    }
}
