// <copyright file="GrpcTransportServiceCollectionExtensions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Transport.Grpc.DependencyInjection
{
    using System;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.Extensions.DependencyInjection;
    using Synaxis.Transport.Grpc.Interceptors;
    using Synaxis.Transport.Grpc.Services;

    /// <summary>
    /// Extension methods for configuring Synaxis gRPC transport services.
    /// </summary>
    public static class GrpcTransportServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Synaxis gRPC transport services to the service collection.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <param name="configureOptions">Optional action to configure gRPC options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddSynaxisTransportGrpc(
            this IServiceCollection services,
            Action<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>? configureOptions = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // Add gRPC services
            services.AddGrpc(options =>
            {
                options.Interceptors.Add<LoggingInterceptor>();
                options.Interceptors.Add<AuthenticationInterceptor>();
            });

            // Configure Kestrel if options provided
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }

            // Register interceptors
            services.AddSingleton<LoggingInterceptor>();
            services.AddSingleton<AuthenticationInterceptor>();

            // Register gRPC services (transient to avoid root provider issues)
            services.AddTransient<ChatGrpcService>();
            services.AddTransient<EmbeddingsGrpcService>();

            return services;
        }

        /// <summary>
        /// Maps Synaxis gRPC transport endpoints to the endpoint route builder.
        /// </summary>
        /// <param name="endpoints">The endpoint route builder to map endpoints to.</param>
        /// <returns>The endpoint route builder for chaining.</returns>
        public static IEndpointRouteBuilder MapSynaxisTransportGrpc(this IEndpointRouteBuilder endpoints)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            endpoints.MapGrpcService<ChatGrpcService>();
            endpoints.MapGrpcService<EmbeddingsGrpcService>();

            return endpoints;
        }
    }
}
