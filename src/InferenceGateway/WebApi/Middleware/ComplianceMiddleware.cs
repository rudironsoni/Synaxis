// <copyright file="ComplianceMiddleware.cs" company="Synaxis">
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
    /// Middleware that enforces data protection compliance (GDPR, LGPD, CCPA).
    /// Validates processing legality and logs compliance events.
    /// </summary>
    public sealed class ComplianceMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ComplianceMiddleware> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComplianceMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware delegate.</param>
        /// <param name="logger">The logger instance.</param>
        public ComplianceMiddleware(
            RequestDelegate next,
            ILogger<ComplianceMiddleware> logger)
        {
            this._next = next ?? throw new ArgumentNullException(nameof(next));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Invokes the middleware to validate compliance.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="tenantContext">The tenant context.</param>
        /// <param name="complianceProvider">The compliance provider.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
#pragma warning disable MA0051 // Method is too long
        public async Task InvokeAsync(
            HttpContext context,
            ITenantContext tenantContext,
            IComplianceProvider complianceProvider)
#pragma warning restore MA0051
        {
            try
            {
                // Skip compliance checks for health and public endpoints
                if (context.Request.Path.StartsWithSegments("/health", StringComparison.Ordinal) ||
                    context.Request.Path.StartsWithSegments("/openapi", StringComparison.Ordinal))
                {
                    await this._next(context).ConfigureAwait(false);
                    return;
                }

                // Only validate if tenant context is established
                if (tenantContext.OrganizationId == null)
                {
                    await this._next(context).ConfigureAwait(false);
                    return;
                }

                // Get user region from context (set by RegionRoutingMiddleware)
                var userRegion = context.Items["UserRegion"] as string;
                var currentRegion = context.Items["CurrentRegion"] as string;
                var isCrossBorder = (bool)(context.Items["IsCrossBorder"] ?? false);

                // Validate cross-border transfer if applicable
                if (isCrossBorder && !string.IsNullOrEmpty(userRegion) && !string.IsNullOrEmpty(currentRegion))
                {
                    var transferContext = new TransferContext
                    {
                        OrganizationId = tenantContext.OrganizationId.Value,
                        UserId = tenantContext.UserId,
                        FromRegion = userRegion,
                        ToRegion = currentRegion,
                        LegalBasis = "SCC",
                        Purpose = "inference_request",
                        DataCategories = new[] { "api_request", "model_inference" },
                        EncryptionUsed = context.Request.IsHttps,
                        UserConsentObtained = tenantContext.UserId.HasValue,
                    };

                    var isAllowed = await complianceProvider.ValidateTransferAsync(transferContext).ConfigureAwait(false);

                    if (!isAllowed)
                    {
                        this._logger.LogWarning(
                            "Compliance validation failed for cross-border transfer. OrgId: {OrgId}, From: {From}, To: {To}",
                            tenantContext.OrganizationId,
                            userRegion,
                            currentRegion);

                        context.Response.StatusCode = StatusCodes.Status451UnavailableForLegalReasons;
                        await context.Response.WriteAsJsonAsync(new
                        {
                            error = new
                            {
                                message = "Request violates data protection regulations",
                                type = "compliance_error",
                                code = "DATA_PROTECTION_VIOLATION",
                                regulation = complianceProvider.RegulationCode,
                            },
                        }).ConfigureAwait(false);
                        return;
                    }

                    // Log the transfer for audit trail
                    await complianceProvider.LogTransferAsync(transferContext).ConfigureAwait(false);
                }

                // Validate processing is allowed
                if (tenantContext.UserId.HasValue)
                {
                    var processingContext = new ProcessingContext
                    {
                        OrganizationId = tenantContext.OrganizationId.Value,
                        UserId = tenantContext.UserId,
                        ProcessingPurpose = "inference_request",
                        LegalBasis = "contract",
                        DataCategories = new[] { "api_request", "model_inference" },
                    };

                    var isProcessingAllowed = await complianceProvider.IsProcessingAllowedAsync(processingContext).ConfigureAwait(false);

                    if (!isProcessingAllowed)
                    {
                        this._logger.LogWarning(
                            "Processing not allowed by compliance provider. UserId: {UserId}, OrgId: {OrgId}",
                            tenantContext.UserId,
                            tenantContext.OrganizationId);

                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsJsonAsync(new
                        {
                            error = new
                            {
                                message = "Data processing not permitted under applicable regulations",
                                type = "compliance_error",
                                code = "PROCESSING_NOT_PERMITTED",
                            },
                        }).ConfigureAwait(false);
                        return;
                    }
                }

                // Add compliance headers
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers["X-Compliance-Framework"] = complianceProvider.RegulationCode;
                    context.Response.Headers["X-Data-Region"] = complianceProvider.Region;
                    return Task.CompletedTask;
                });

                await this._next(context).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error occurred during compliance validation");
                await this._next(context).ConfigureAwait(false);
            }
        }
    }
}
