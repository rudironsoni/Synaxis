// <copyright file="FailoverMiddleware.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Middleware
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Synaxis.Core.Contracts;
    using Synaxis.InferenceGateway.Application.Interfaces;

    /// <summary>
    /// Middleware that handles regional failover scenarios.
    /// Routes to healthy regions when primary region is unavailable.
    /// </summary>
    public sealed class FailoverMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<FailoverMiddleware> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="FailoverMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware delegate.</param>
        /// <param name="logger">The logger instance.</param>
        public FailoverMiddleware(
            RequestDelegate next,
            ILogger<FailoverMiddleware> logger)
        {
            this._next = next!;
            this._logger = logger!;
        }

        /// <summary>
        /// Invokes the middleware to handle failover.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="tenantContext">The tenant context.</param>
        /// <param name="failoverService">The failover service.</param>
        /// <param name="healthMonitor">The health monitor.</param>
        /// <param name="regionRouter">The region router.</param>
        /// <param name="geoIPService">The GeoIP service.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InvokeAsync(
            HttpContext context,
            ITenantContext tenantContext,
            IFailoverService failoverService,
            IHealthMonitor healthMonitor,
            IRegionRouter regionRouter,
            IGeoIPService geoIPService)
        {
            try
            {
                // Skip failover for health checks
                if (context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase) ||
                    context.Request.Path.StartsWithSegments("/openapi", StringComparison.OrdinalIgnoreCase))
                {
                    await this._next(context).ConfigureAwait(false);
                    return;
                }

                var currentRegion = Environment.GetEnvironmentVariable("SYNAXIS_REGION") ?? "us-east-1";

                // Check if current region is healthy
                var isHealthy = await healthMonitor.IsRegionHealthyAsync(currentRegion).ConfigureAwait(false);

                if (!isHealthy)
                {
                    this._logger.LogWarning(
                        "Current region {Region} is unhealthy. Attempting failover for OrgId: {OrgId}",
                        currentRegion,
                        tenantContext.OrganizationId);

                    // Get user's location for nearest failover
                    var clientIp = GetClientIpAddress(context);
                    var geoLocation = await geoIPService.GetLocationAsync(clientIp).ConfigureAwait(false);

                    // Get nearest healthy region
                    var failoverRegion = await regionRouter.GetNearestHealthyRegionAsync(currentRegion, geoLocation).ConfigureAwait(false);

                    if (string.IsNullOrEmpty(failoverRegion))
                    {
                        this._logger.LogError(
                            "No healthy regions available for failover. Current: {CurrentRegion}",
                            currentRegion);

                        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                        await context.Response.WriteAsJsonAsync(new
                        {
                            error = new
                            {
                                message = "Service temporarily unavailable in all regions",
                                type = "service_unavailable",
                                code = "NO_HEALTHY_REGIONS",
                            },
                        }).ConfigureAwait(false);
                        return;
                    }

                    this._logger.LogInformation(
                        "Failing over from {CurrentRegion} to {FailoverRegion}",
                        currentRegion,
                        failoverRegion);

                    // Update context with failover info
                    context.Items["FailoverActive"] = true;
                    context.Items["FailoverFrom"] = currentRegion;
                    context.Items["FailoverTo"] = failoverRegion;

                    // Add failover headers
                    context.Response.OnStarting(() =>
                    {
                        context.Response.Headers["X-Synaxis-Failover"] = "true";
                        context.Response.Headers["X-Synaxis-Failover-From"] = currentRegion;
                        context.Response.Headers["X-Synaxis-Failover-To"] = failoverRegion;
                        return Task.CompletedTask;
                    });

                    // If user has data residency requirements, check consent for cross-border failover
                    if (tenantContext.UserId.HasValue)
                    {
                        var userRegion = await regionRouter.GetUserRegionAsync(tenantContext.UserId.Value).ConfigureAwait(false);

                        if (!string.Equals(userRegion, failoverRegion, StringComparison.Ordinal))
                        {
                            var requiresConsent = await regionRouter.RequiresCrossBorderConsentAsync(
                                tenantContext.UserId.Value,
                                failoverRegion).ConfigureAwait(false);

                            if (requiresConsent)
                            {
                                this._logger.LogWarning(
                                    "Failover to {FailoverRegion} requires consent. UserId: {UserId}, UserRegion: {UserRegion}",
                                    failoverRegion,
                                    tenantContext.UserId.Value,
                                    userRegion);

                                context.Response.StatusCode = StatusCodes.Status451UnavailableForLegalReasons;
                                await context.Response.WriteAsJsonAsync(new
                                {
                                    error = new
                                    {
                                        message = "Failover requires cross-border consent",
                                        type = "consent_required",
                                        code = "FAILOVER_CONSENT_REQUIRED",
                                        user_region = userRegion,
                                        failover_region = failoverRegion,
                                        reason = "primary_region_unavailable",
                                    },
                                }).ConfigureAwait(false);
                                return;
                            }

                            // Log cross-border failover transfer
                            await regionRouter.LogCrossBorderTransferAsync(new CrossBorderTransferContext
                            {
                                OrganizationId = tenantContext.OrganizationId!.Value,
                                UserId = tenantContext.UserId,
                                FromRegion = userRegion,
                                ToRegion = failoverRegion,
                                LegalBasis = "vital_interest", // Failover is for service continuity
                                Purpose = "disaster_recovery",
                                DataCategories = new[] { "api_request", "model_inference" },
                            }).ConfigureAwait(false);
                        }
                    }
                }

                await this._next(context).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error occurred during failover handling");
                await this._next(context).ConfigureAwait(false);
            }
        }

        private static string GetClientIpAddress(HttpContext context)
        {
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }
}
