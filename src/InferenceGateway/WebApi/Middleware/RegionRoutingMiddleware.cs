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
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Invokes the middleware to determine routing region.
    /// </summary>
    public async Task InvokeAsync(
        HttpContext context,
        ITenantContext tenantContext,
        IRegionRouter regionRouter,
        IGeoIPService geoIPService)
    {
        try
        {
            // Skip region routing for health checks and public endpoints
            if (context.Request.Path.StartsWithSegments("/health") ||
                context.Request.Path.StartsWithSegments("/openapi"))
            {
                await _next(context);
                return;
            }

            // Only route if tenant context is established
            if (tenantContext.OrganizationId == null)
            {
                await _next(context);
                return;
            }

            // Get current server region from configuration
            var currentRegion = Environment.GetEnvironmentVariable("SYNAXIS_REGION") ?? "us-east-1";

            // Determine user's data residency region
            string userRegion;
            if (tenantContext.UserId.HasValue)
            {
                userRegion = await regionRouter.GetUserRegionAsync(tenantContext.UserId.Value);
            }
            else
            {
                // For API key requests, detect region from IP if not specified
                var clientIp = GetClientIpAddress(context);
                var geoLocation = await geoIPService.GetLocationAsync(clientIp);
                userRegion = MapCountryToRegion(geoLocation.CountryCode);
            }

            // Store routing info in context
            context.Items["UserRegion"] = userRegion;
            context.Items["CurrentRegion"] = currentRegion;
            context.Items["IsCrossBorder"] = await regionRouter.IsCrossBorderAsync(currentRegion, userRegion);

            // Check if cross-border routing is required
            if (currentRegion != userRegion)
            {
                _logger.LogInformation(
                    "Cross-region request detected. Current: {CurrentRegion}, User: {UserRegion}, OrgId: {OrgId}",
                    currentRegion, userRegion, tenantContext.OrganizationId);

                // Check if consent is required
                if (tenantContext.UserId.HasValue)
                {
                    var requiresConsent = await regionRouter.RequiresCrossBorderConsentAsync(
                        tenantContext.UserId.Value,
                        currentRegion);

                    if (requiresConsent)
                    {
                        _logger.LogWarning(
                            "Cross-border transfer requires consent. UserId: {UserId}, From: {From}, To: {To}",
                            tenantContext.UserId.Value, userRegion, currentRegion);

                        context.Response.StatusCode = StatusCodes.Status451UnavailableForLegalReasons;
                        await context.Response.WriteAsJsonAsync(new
                        {
                            error = new
                            {
                                message = "Cross-border data transfer requires user consent",
                                type = "consent_required",
                                code = "CROSS_BORDER_CONSENT_REQUIRED",
                                user_region = userRegion,
                                current_region = currentRegion
                            }
                        });
                        return;
                    }
                }

                // Log cross-border transfer for compliance audit
                await regionRouter.LogCrossBorderTransferAsync(new CrossBorderTransferContext
                {
                    OrganizationId = tenantContext.OrganizationId.Value,
                    UserId = tenantContext.UserId,
                    FromRegion = userRegion,
                    ToRegion = currentRegion,
                    LegalBasis = "SCC", // Standard Contractual Clauses
                    Purpose = "inference_request",
                    DataCategories = new[] { "api_request", "model_inference" }
                });
            }

            // Add region headers for debugging
            context.Response.OnStarting(() =>
            {
                context.Response.Headers["X-Synaxis-Region"] = currentRegion;
                context.Response.Headers["X-User-Region"] = userRegion;
                context.Response.Headers["X-Cross-Border"] = (currentRegion != userRegion).ToString();
                return Task.CompletedTask;
            });

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during region routing");
            await _next(context);
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

    private static string MapCountryToRegion(string countryCode)
    {
        // EU countries
        var euCountries = new HashSet<string>
        {
            "AT", "BE", "BG", "HR", "CY", "CZ", "DK", "EE", "FI", "FR", "DE", "GR", "HU", "IE",
            "IT", "LV", "LT", "LU", "MT", "NL", "PL", "PT", "RO", "SK", "SI", "ES", "SE", "GB"
        };

        if (euCountries.Contains(countryCode))
            return "eu-west-1";

        if (countryCode == "BR")
            return "sa-east-1";

        // Default to US
        return "us-east-1";
    }
    }

}