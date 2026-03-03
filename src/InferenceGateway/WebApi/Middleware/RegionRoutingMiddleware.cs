// <copyright file="RegionRoutingMiddleware.cs" company="Synaxis">
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
    /// Middleware that routes requests to the appropriate region based on user data residency.
    /// Handles multi-region routing and failover scenarios for compliance with GDPR/LGPD.
    /// </summary>
    public sealed class RegionRoutingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RegionRoutingMiddleware> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegionRoutingMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware delegate.</param>
        /// <param name="logger">The logger instance.</param>
        public RegionRoutingMiddleware(
            RequestDelegate next,
            ILogger<RegionRoutingMiddleware> logger)
        {
            this._next = next!;
            this._logger = logger!;
        }

        /// <summary>
        /// Invokes the middleware to determine routing region.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="tenantContext">The tenant context.</param>
        /// <param name="regionRouter">The region router.</param>
        /// <param name="geoIPService">The GeoIP service.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InvokeAsync(
            HttpContext context,
            ITenantContext tenantContext,
            IRegionRouter regionRouter,
            IGeoIPService geoIPService)
        {
            try
            {
                if (RegionRoutingMiddleware.ShouldSkip(context, tenantContext))
                {
                    await this._next(context).ConfigureAwait(false);
                    return;
                }

                var currentRegion = RegionRoutingMiddleware.ResolveCurrentRegion();
                var userRegion = await ResolveUserRegionAsync(context, tenantContext, regionRouter, geoIPService).ConfigureAwait(false);

                context.Items["UserRegion"] = userRegion;
                context.Items["CurrentRegion"] = currentRegion;
                context.Items["IsCrossBorder"] = await regionRouter.IsCrossBorderAsync(currentRegion, userRegion).ConfigureAwait(false);

                if (!string.Equals(currentRegion, userRegion, StringComparison.Ordinal))
                {
                    var shouldContinue = await this.HandleCrossBorderAsync(context, tenantContext, regionRouter, userRegion, currentRegion).ConfigureAwait(false);
                    if (!shouldContinue)
                    {
                        return;
                    }
                }

                RegionRoutingMiddleware.AddRegionHeaders(context, currentRegion, userRegion);
                await this._next(context).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error occurred during region routing");
                await this._next(context).ConfigureAwait(false);
            }
        }

        private static bool ShouldSkip(HttpContext context, ITenantContext tenantContext)
        {
            if (context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase) ||
                context.Request.Path.StartsWithSegments("/openapi", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return tenantContext.OrganizationId == null;
        }

        private static string ResolveCurrentRegion()
        {
            return Environment.GetEnvironmentVariable("SYNAXIS_REGION") ?? "us-east-1";
        }

        private static void AddRegionHeaders(HttpContext context, string currentRegion, string userRegion)
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers["X-Synaxis-Region"] = currentRegion;
                context.Response.Headers["X-User-Region"] = userRegion;
                context.Response.Headers["X-Cross-Border"] = (!string.Equals(currentRegion, userRegion, StringComparison.Ordinal)).ToString();
                return Task.CompletedTask;
            });
        }

        private static async Task<string> ResolveUserRegionAsync(
            HttpContext context,
            ITenantContext tenantContext,
            IRegionRouter regionRouter,
            IGeoIPService geoIPService)
        {
            if (tenantContext.UserId.HasValue)
            {
                return await regionRouter.GetUserRegionAsync(tenantContext.UserId.Value).ConfigureAwait(false);
            }

            var clientIp = GetClientIpAddress(context);
            var geoLocation = await geoIPService.GetLocationAsync(clientIp).ConfigureAwait(false);
            return MapCountryToRegion(geoLocation.CountryCode);
        }

        private async Task<bool> HandleCrossBorderAsync(
            HttpContext context,
            ITenantContext tenantContext,
            IRegionRouter regionRouter,
            string userRegion,
            string currentRegion)
        {
            this._logger.LogInformation(
                "Cross-region request detected. Current: {CurrentRegion}, User: {UserRegion}, OrgId: {OrgId}",
                currentRegion,
                userRegion,
                tenantContext.OrganizationId);

            if (tenantContext.UserId.HasValue)
            {
                var requiresConsent = await regionRouter.RequiresCrossBorderConsentAsync(
                    tenantContext.UserId.Value,
                    currentRegion).ConfigureAwait(false);

                if (requiresConsent)
                {
                    this._logger.LogWarning(
                        "Cross-border transfer requires consent. UserId: {UserId}, From: {From}, To: {To}",
                        tenantContext.UserId.Value,
                        userRegion,
                        currentRegion);

                    context.Response.StatusCode = StatusCodes.Status451UnavailableForLegalReasons;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = new
                        {
                            message = "Cross-border data transfer requires user consent",
                            type = "consent_required",
                            code = "CROSS_BORDER_CONSENT_REQUIRED",
                            user_region = userRegion,
                            current_region = currentRegion,
                        },
                    }).ConfigureAwait(false);
                    return false;
                }
            }

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
                ToRegion = currentRegion,
                LegalBasis = "SCC", // Standard Contractual Clauses
                Purpose = "inference_request",
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

        private static string MapCountryToRegion(string countryCode)
        {
            // EU countries
            var euCountries = new HashSet<string>(StringComparer.Ordinal)
        {
            "AT", "BE", "BG", "HR", "CY", "CZ", "DK", "EE", "FI", "FR", "DE", "GR", "HU", "IE",
            "IT", "LV", "LT", "LU", "MT", "NL", "PL", "PT", "RO", "SK", "SI", "ES", "SE", "GB",
        };

            if (euCountries.Contains(countryCode))
            {
                return "eu-west-1";
            }

            if (string.Equals(countryCode, "BR", StringComparison.Ordinal))
            {
                return "sa-east-1";
            }

            // Default to US
            return "us-east-1";
        }
    }
}
