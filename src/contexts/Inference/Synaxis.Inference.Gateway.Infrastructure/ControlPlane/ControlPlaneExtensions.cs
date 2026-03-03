// <copyright file="ControlPlaneExtensions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Synaxis.InferenceGateway.Application.ControlPlane;

    /// <summary>
    /// ControlPlaneExtensions class.
    /// </summary>
    public static class ControlPlaneExtensions
    {
        /// <summary>
        /// Adds control plane services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddControlPlane(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ControlPlaneDbContext>((sp, builder) =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var logger = sp.GetService<ILogger<ControlPlaneDbContext>>();
                var options = new ControlPlaneOptions();
                config.GetSection("Synaxis:ControlPlane").Bind(options);

                logger?.LogInformation(
                    "ControlPlane configuration - UseInMemory: {UseInMemory}, ConnectionString present: {HasConnectionString}",
                    options.UseInMemory,
                    !string.IsNullOrWhiteSpace(options.ConnectionString));

                if (!string.IsNullOrWhiteSpace(options.ConnectionString))
                {
                    // Mask password for logging
                    var connStrMasked = MaskPassword(options.ConnectionString);
                    logger?.LogInformation("Using PostgreSQL with connection string: {ConnectionString}", connStrMasked);
                }

                if (options.UseInMemory || string.IsNullOrWhiteSpace(options.ConnectionString))
                {
                    logger?.LogWarning("Using in-memory database for ControlPlane");
                    builder.UseInMemoryDatabase("SynaxisControlPlane");
                }
                else
                {
                    builder.UseNpgsql(options.ConnectionString);
                }
            });

            // Register SynaxisDbContext for Identity
            services.AddDbContext<SynaxisDbContext>((sp, builder) =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var logger = sp.GetService<ILogger<SynaxisDbContext>>();
                var options = new ControlPlaneOptions();
                config.GetSection("Synaxis:ControlPlane").Bind(options);

                if (options.UseInMemory || string.IsNullOrWhiteSpace(options.ConnectionString))
                {
                    logger?.LogWarning("Using in-memory database for Identity");
                    builder.UseInMemoryDatabase("SynaxisIdentity");
                }
                else
                {
                    builder.UseNpgsql(options.ConnectionString);
                }
            });

            services.AddScoped<IDeviationRegistry, DeviationRegistry>();

            return services;
        }

        private static string MaskPassword(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return string.Empty;
            }

            var segments = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            for (var index = 0; index < segments.Length; index++)
            {
                var parts = segments[index].Split('=', 2);
                if (parts.Length != 2)
                {
                    continue;
                }

                if (string.Equals(parts[0], "Password", StringComparison.OrdinalIgnoreCase))
                {
                    segments[index] = $"{parts[0]}=***";
                    break;
                }
            }

            return string.Join(';', segments);
        }
    }
}
