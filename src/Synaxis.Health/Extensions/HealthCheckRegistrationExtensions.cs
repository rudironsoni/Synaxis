// <copyright file="HealthCheckRegistrationExtensions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Health.Extensions
{
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Microsoft.Extensions.Logging;
    using StackExchange.Redis;
    using Synaxis.Health.Checks;
    using Synaxis.Health.Configuration;

    /// <summary>
    /// Extension methods for registering health checks.
    /// </summary>
    public static class HealthCheckRegistrationExtensions
    {
        /// <summary>
        /// Adds all Synaxis health checks to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The health checks builder for chaining.</returns>
        public static IHealthChecksBuilder AddSynaxisHealthChecks(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var healthCheckOptions = configuration.GetSection("HealthChecks").Get<SynaxisHealthCheckOptions>()
                ?? new SynaxisHealthCheckOptions();

            if (!healthCheckOptions.Enabled)
            {
                return services.AddHealthChecks();
            }

            var builder = services.AddHealthChecks();
            var timeout = TimeSpan.FromSeconds(healthCheckOptions.TimeoutSeconds);

            // Add database health check
            builder.AddDatabaseHealthCheck(configuration, timeout);

            // Add Redis health check
            builder.AddRedisHealthCheck(configuration, timeout);

            // Add service discovery health check
            builder.AddServiceDiscoveryHealthCheck(timeout);

            return builder;
        }

        /// <summary>
        /// Adds the PostgreSQL database health check.
        /// </summary>
        /// <param name="builder">The health checks builder.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="timeout">The health check timeout.</param>
        /// <returns>The health checks builder for chaining.</returns>
        public static IHealthChecksBuilder AddDatabaseHealthCheck(
            this IHealthChecksBuilder builder,
            IConfiguration configuration,
            TimeSpan? timeout = null)
        {
            var options = configuration.GetSection("HealthChecks").Get<SynaxisHealthCheckOptions>()
                ?? new SynaxisHealthCheckOptions();

            var connectionStringName = options.PostgreSqlConnectionStringName;
            var connectionString = configuration.GetConnectionString(connectionStringName);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return builder;
            }

            return builder.Add(new HealthCheckRegistration(
                "database",
                serviceProvider => new DatabaseHealthCheck(
                    connectionString,
                    serviceProvider.GetRequiredService<ILogger<DatabaseHealthCheck>>()),
                HealthStatus.Unhealthy,
                new[] { "database", "postgresql", "layer2" },
                timeout));
        }

        /// <summary>
        /// Adds the Redis health check.
        /// </summary>
        /// <param name="builder">The health checks builder.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="timeout">The health check timeout.</param>
        /// <returns>The health checks builder for chaining.</returns>
        public static IHealthChecksBuilder AddRedisHealthCheck(
            this IHealthChecksBuilder builder,
            IConfiguration configuration,
            TimeSpan? timeout = null)
        {
            var options = configuration.GetSection("HealthChecks").Get<SynaxisHealthCheckOptions>()
                ?? new SynaxisHealthCheckOptions();

            var connectionStringName = options.RedisConnectionStringName;
            var connectionString = configuration.GetConnectionString(connectionStringName);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return builder;
            }

            return builder.Add(new HealthCheckRegistration(
                "redis",
                serviceProvider =>
                {
                    var connectionMultiplexer = serviceProvider.GetService<IConnectionMultiplexer>();
                    if (connectionMultiplexer == null)
                    {
                        // Try to create a new connection
                        connectionMultiplexer = ConnectionMultiplexer.Connect(connectionString);
                    }

                    return new RedisHealthCheck(
                        connectionMultiplexer,
                        serviceProvider.GetRequiredService<ILogger<RedisHealthCheck>>());
                },
                HealthStatus.Unhealthy,
                new[] { "redis", "cache", "layer2" },
                timeout));
        }

        /// <summary>
        /// Adds the service discovery health check.
        /// </summary>
        /// <param name="builder">The health checks builder.</param>
        /// <param name="timeout">The health check timeout.</param>
        /// <returns>The health checks builder for chaining.</returns>
        public static IHealthChecksBuilder AddServiceDiscoveryHealthCheck(
            this IHealthChecksBuilder builder,
            TimeSpan? timeout = null)
        {
            return builder.Add(new HealthCheckRegistration(
                "service-discovery",
                serviceProvider =>
                {
                    var discoveryClient = serviceProvider.GetService<IServiceDiscoveryClient>();
                    if (discoveryClient == null)
                    {
                        // Return a health check that reports degraded when no client is available
                        return new NoOpServiceDiscoveryHealthCheck();
                    }

                    return new ServiceDiscoveryHealthCheck(
                        discoveryClient,
                        serviceProvider.GetRequiredService<ILogger<ServiceDiscoveryHealthCheck>>());
                },
                HealthStatus.Degraded,
                new[] { "service-discovery", "registry", "layer2" },
                timeout));
        }

        /// <summary>
        /// Adds health check services to the DI container.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddSynaxisHealthServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<SynaxisHealthCheckOptions>(
                configuration.GetSection("HealthChecks"));

            return services;
        }
    }
}
