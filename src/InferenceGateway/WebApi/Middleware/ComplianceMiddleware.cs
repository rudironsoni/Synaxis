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
            this._next = next!;
            this._logger = logger!;
        }

        /// <summary>
        /// Invokes the middleware to validate compliance.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="tenantContext">The tenant context.</param>
        /// <param name="complianceProvider">The compliance provider.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task InvokeAsync(
            HttpContext context,
            ITenantContext tenantContext,
            IComplianceProvider complianceProvider)
        {
            try
            {
                if (ShouldSkipCompliance(context, tenantContext))
                {
                    await this._next(context).ConfigureAwait(false);
                    return;
                }

                var transferContext = BuildTransferContext(context, tenantContext);
                if (transferContext != null)
                {
                    var shouldContinue = await this.ValidateTransferAsync(context, complianceProvider, transferContext).ConfigureAwait(false);
                    if (!shouldContinue)
                    {
                        return;
                    }
                }

                if (tenantContext.UserId.HasValue)
                {
                    var processingContext = BuildProcessingContext(tenantContext);
                    var isProcessingAllowed = await complianceProvider.IsProcessingAllowedAsync(processingContext).ConfigureAwait(false);
                    if (!isProcessingAllowed)
                    {
                        await this.WriteProcessingNotPermittedAsync(context, tenantContext).ConfigureAwait(false);
                        return;
                    }
                }

                AddComplianceHeaders(context, complianceProvider);
                await this._next(context).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error occurred during compliance validation");
                await this._next(context).ConfigureAwait(false);
            }
        }

        private static bool ShouldSkipCompliance(HttpContext context, ITenantContext tenantContext)
        {
            if (context.Request.Path.StartsWithSegments("/health", StringComparison.Ordinal) ||
                context.Request.Path.StartsWithSegments("/openapi", StringComparison.Ordinal))
            {
                return true;
            }

            return tenantContext.OrganizationId == null;
        }

        private static TransferContext? BuildTransferContext(HttpContext context, ITenantContext tenantContext)
        {
            var userRegion = context.Items["UserRegion"] as string;
            var currentRegion = context.Items["CurrentRegion"] as string;
            var isCrossBorder = (bool)(context.Items["IsCrossBorder"] ?? false);

            if (!isCrossBorder || string.IsNullOrEmpty(userRegion) || string.IsNullOrEmpty(currentRegion))
            {
                return null;
            }

            var organizationId = tenantContext.OrganizationId;
            if (!organizationId.HasValue)
            {
                return null;
            }

            return new TransferContext
            {
                OrganizationId = organizationId.Value,
                UserId = tenantContext.UserId,
                FromRegion = userRegion,
                ToRegion = currentRegion,
                LegalBasis = "SCC",
                Purpose = "inference_request",
                DataCategories = new[] { "api_request", "model_inference" },
                EncryptionUsed = context.Request.IsHttps,
                UserConsentObtained = tenantContext.UserId.HasValue,
            };
        }

        private async Task<bool> ValidateTransferAsync(
            HttpContext context,
            IComplianceProvider complianceProvider,
            TransferContext transferContext)
        {
            var isAllowed = await complianceProvider.ValidateTransferAsync(transferContext).ConfigureAwait(false);

            if (isAllowed)
            {
                await complianceProvider.LogTransferAsync(transferContext).ConfigureAwait(false);
                return true;
            }

            this._logger.LogWarning(
                "Compliance validation failed for cross-border transfer. OrgId: {OrgId}, From: {From}, To: {To}",
                transferContext.OrganizationId,
                transferContext.FromRegion,
                transferContext.ToRegion);

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
            return false;
        }

        private static ProcessingContext BuildProcessingContext(ITenantContext tenantContext)
        {
            return new ProcessingContext
            {
                OrganizationId = tenantContext.OrganizationId!.Value,
                UserId = tenantContext.UserId,
                ProcessingPurpose = "inference_request",
                LegalBasis = "contract",
                DataCategories = new[] { "api_request", "model_inference" },
            };
        }

        private Task WriteProcessingNotPermittedAsync(HttpContext context, ITenantContext tenantContext)
        {
            this._logger.LogWarning(
                "Processing not allowed by compliance provider. UserId: {UserId}, OrgId: {OrgId}",
                tenantContext.UserId,
                tenantContext.OrganizationId);

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return context.Response.WriteAsJsonAsync(new
            {
                error = new
                {
                    message = "Data processing not permitted under applicable regulations",
                    type = "compliance_error",
                    code = "PROCESSING_NOT_PERMITTED",
                },
            });
        }

        private static void AddComplianceHeaders(HttpContext context, IComplianceProvider complianceProvider)
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers["X-Compliance-Framework"] = complianceProvider.RegulationCode;
                context.Response.Headers["X-Data-Region"] = complianceProvider.Region;
                return Task.CompletedTask;
            });
        }
    }
}
