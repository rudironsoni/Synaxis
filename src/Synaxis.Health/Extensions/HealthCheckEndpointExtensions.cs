// <copyright file="HealthCheckEndpointExtensions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Health.Extensions
{
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Diagnostics.HealthChecks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Synaxis.Health.Configuration;

    /// <summary>
    /// Extension methods for mapping health check endpoints.
    /// </summary>
    public static class HealthCheckEndpointExtensions
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true,
        };

        /// <summary>
        /// Maps Synaxis health check endpoints.
        /// </summary>
        /// <param name="endpoints">The endpoint route builder.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The endpoint route builder for chaining.</returns>
        public static IEndpointRouteBuilder MapSynaxisHealthChecks(
            this IEndpointRouteBuilder endpoints,
            IConfiguration configuration)
        {
            var options = configuration.GetSection("HealthChecks").Get<SynaxisHealthCheckOptions>()
                ?? new SynaxisHealthCheckOptions();

            if (!options.Enabled)
            {
                return endpoints;
            }

            MapOverallHealthEndpoint(endpoints);
            MapLivenessEndpoint(endpoints);
            MapReadinessEndpoint(endpoints);
            MapDetailedHealthEndpoint(endpoints, options);

            return endpoints;
        }

        private static void MapOverallHealthEndpoint(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = WriteHealthResponse,
                ResultStatusCodes =
                {
                    [HealthStatus.Healthy] = StatusCodes.Status200OK,
                    [HealthStatus.Degraded] = StatusCodes.Status200OK,
                    [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
                },
            }).AllowAnonymous();
        }

        private static void MapLivenessEndpoint(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = _ => false, // No checks, just verify app is running
                ResponseWriter = WriteLivenessResponse,
            }).AllowAnonymous();
        }

        private static void MapReadinessEndpoint(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("layer2", StringComparer.Ordinal),
                ResponseWriter = WriteHealthResponse,
                ResultStatusCodes =
                {
                    [HealthStatus.Healthy] = StatusCodes.Status200OK,
                    [HealthStatus.Degraded] = StatusCodes.Status200OK,
                    [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
                },
            }).AllowAnonymous();
        }

        private static void MapDetailedHealthEndpoint(
            IEndpointRouteBuilder endpoints,
            SynaxisHealthCheckOptions options)
        {
            var detailedHealthEndpoint = endpoints.MapHealthChecks("/health/detailed", new HealthCheckOptions
            {
                ResponseWriter = WriteDetailedHealthResponse,
                ResultStatusCodes =
                {
                    [HealthStatus.Healthy] = StatusCodes.Status200OK,
                    [HealthStatus.Degraded] = StatusCodes.Status200OK,
                    [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
                },
            });

            if (options.RequireAuthenticationForDetailedHealth)
            {
                detailedHealthEndpoint.RequireAuthorization();
            }
            else
            {
                detailedHealthEndpoint.AllowAnonymous();
            }
        }

        /// <summary>
        /// Writes the standard health response.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="result">The health report.</param>
        /// <returns>A task representing the write operation.</returns>
        private static Task WriteHealthResponse(HttpContext context, HealthReport result)
        {
            context.Response.ContentType = "application/json";

            var response = new
            {
                status = result.Status.ToString(),
                totalDuration = result.TotalDuration.ToString(),
                entries = result.Entries.ToDictionary(
                    entry => entry.Key,
                    entry => new
                    {
                        status = entry.Value.Status.ToString(),
                        duration = entry.Value.Duration.ToString(),
                        data = entry.Value.Data.Count > 0 ? entry.Value.Data : null,
                    },
                    StringComparer.Ordinal),
            };

            return context.Response.WriteAsJsonAsync(response, JsonOptions);
        }

        /// <summary>
        /// Writes the detailed health response with additional information.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="result">The health report.</param>
        /// <returns>A task representing the write operation.</returns>
        private static Task WriteDetailedHealthResponse(HttpContext context, HealthReport result)
        {
            context.Response.ContentType = "application/json";

            var response = new
            {
                status = result.Status.ToString(),
                totalDuration = result.TotalDuration.ToString(),
                timestamp = DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
                environment = context.RequestServices.GetService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>()?.EnvironmentName,
                entries = result.Entries.ToDictionary(
                    entry => entry.Key,
                    entry => new
                    {
                        status = entry.Value.Status.ToString(),
                        duration = entry.Value.Duration.ToString(),
                        description = entry.Value.Description,
                        exception = entry.Value.Exception?.Message,
                        data = entry.Value.Data.Count > 0 ? entry.Value.Data : null,
                        tags = entry.Value.Tags.ToList(),
                    },
                    StringComparer.Ordinal),
            };

            return context.Response.WriteAsJsonAsync(response, JsonOptions);
        }

        /// <summary>
        /// Writes the liveness probe response.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="result">The health report.</param>
        /// <returns>A task representing the write operation.</returns>
        private static Task WriteLivenessResponse(HttpContext context, HealthReport result)
        {
            context.Response.ContentType = "application/json";

            var response = new
            {
                status = "Alive",
                timestamp = DateTime.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            };

            return context.Response.WriteAsJsonAsync(response, JsonOptions);
        }

        /// <summary>
        /// Adds health check endpoints to the web application.
        /// </summary>
        /// <param name="app">The web application.</param>
        /// <param name="configuration">The configuration.</param>
        /// <returns>The web application for chaining.</returns>
        public static IApplicationBuilder UseSynaxisHealthChecks(
            this IApplicationBuilder app,
            IConfiguration configuration)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapSynaxisHealthChecks(configuration);
            });

            return app;
        }
    }
}
