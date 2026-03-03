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
                if (ShouldSkip(context))
                {
                    await this._next(context).ConfigureAwait(false);
                    return;
                }

                var currentRegion = ResolveCurrentRegion();
                var isHealthy = await healthMonitor.IsRegionHealthyAsync(currentRegion).ConfigureAwait(false);

                if (!isHealthy)
                {
                    var failoverRegion = await this.ResolveFailoverRegionAsync(tenantContext, context, geoIPService, regionRouter, currentRegion).ConfigureAwait(false);
                    if (string.IsNullOrEmpty(failoverRegion))
                    {
                        await this.WriteNoHealthyRegionAsync(context, currentRegion).ConfigureAwait(false);
                        return;
                    }

                    SetFailoverContext(context, currentRegion, failoverRegion);

                    var shouldContinue = await this.HandleCrossBorderFailoverAsync(context, tenantContext, regionRouter, failoverRegion).ConfigureAwait(false);
                    if (!shouldContinue)
                    {
                        return;
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

        private static bool ShouldSkip(HttpContext context)
        {
            return context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase) ||
                   context.Request.Path.StartsWithSegments("/openapi", StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveCurrentRegion()
        {
            return Environment.GetEnvironmentVariable("SYNAXIS_REGION") ?? "us-east-1";
        }

        private async Task<string?> ResolveFailoverRegionAsync(
            ITenantContext tenantContext,
            HttpContext context,
            IGeoIPService geoIPService,
            IRegionRouter regionRouter,
            string currentRegion)
        {
            this._logger.LogWarning(
                "Current region {Region} is unhealthy. Attempting failover for OrgId: {OrgId}",
                currentRegion,
                tenantContext.OrganizationId);

            var clientIp = GetClientIpAddress(context);
            var geoLocation = await geoIPService.GetLocationAsync(clientIp).ConfigureAwait(false);
            return await regionRouter.GetNearestHealthyRegionAsync(currentRegion, geoLocation).ConfigureAwait(false);
        }

        private Task WriteNoHealthyRegionAsync(HttpContext context, string currentRegion)
        {
            this._logger.LogError(
                "No healthy regions available for failover. Current: {CurrentRegion}",
                currentRegion);

            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return context.Response.WriteAsJsonAsync(new
            {
                error = new
                {
                    message = "Service temporarily unavailable in all regions",
                    type = "service_unavailable",
                    code = "NO_HEALTHY_REGIONS",
                },
            });
        }

        private static void SetFailoverContext(HttpContext context, string currentRegion, string failoverRegion)
        {
            context.Items["FailoverActive"] = true;
            context.Items["FailoverFrom"] = currentRegion;
            context.Items["FailoverTo"] = failoverRegion;

            context.Response.OnStarting(() =>
            {
                context.Response.Headers["X-Synaxis-Failover"] = "true";
                context.Response.Headers["X-Synaxis-Failover-From"] = currentRegion;
                context.Response.Headers["X-Synaxis-Failover-To"] = failoverRegion;
                return Task.CompletedTask;
            });
        }

        private async Task<bool> HandleCrossBorderFailoverAsync(
            HttpContext context,
            ITenantContext tenantContext,
            IRegionRouter regionRouter,
            string failoverRegion)
        {
            if (!tenantContext.UserId.HasValue)
            {
                return true;
            }

            var userRegion = await regionRouter.GetUserRegionAsync(tenantContext.UserId.Value).ConfigureAwait(false);

            if (string.Equals(userRegion, failoverRegion, StringComparison.Ordinal))
            {
                return true;
            }

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
                return false;
            }

            return await this.LogCrossBorderFailoverAsync(tenantContext, regionRouter, userRegion, failoverRegion).ConfigureAwait(false);
        }

        private async Task<bool> LogCrossBorderFailoverAsync(
            ITenantContext tenantContext,
            IRegionRouter regionRouter,
            string userRegion,
            string failoverRegion)
        {
            var organizationId = tenantContext.OrganizationId;
            if (!organizationId.HasValue)
            {
                return true;
            }

            await regionRouter.LogCrossBorderTransferAsync(new CrossBorderTransferContext
            {
                OrganizationId = organizationId.Value,
                UserId = tenantContext.UserId,
                FromRegion = userRegion,
                ToRegion = failoverRegion,
                LegalBasis = "vital_interest",
                Purpose = "disaster_recovery",
                DataCategories = new[] { "api_request", "model_inference" },
            }).ConfigureAwait(false);

            return true;
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
