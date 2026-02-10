// <copyright file="SuperAdminController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.Api.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Synaxis.Core.Contracts;

    /// <summary>
    /// Super Admin controller for cross-region visibility and management
    /// Requires SuperAdmin role, TOTP MFA, IP allowlist, and business hours access.
    /// </summary>
    [ApiController]
    [Route("admin/super")]
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminController : ControllerBase
    {
        private readonly ISuperAdminService _superAdminService;
        private readonly ILogger<SuperAdminController> _logger;

        public SuperAdminController(
            ISuperAdminService superAdminService,
            ILogger<SuperAdminController> logger)
        {
            this._superAdminService = superAdminService ?? throw new ArgumentNullException(nameof(superAdminService));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get all organizations across all regions.
        /// </summary>
        /// <remarks>
        /// WARNING: This endpoint exposes cross-region data. All access is audited.
        /// </remarks>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpGet("organizations")]
        [ProducesResponseType(typeof(List<OrganizationSummary>), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetCrossRegionOrganizations(
            [FromHeader(Name = "X-MFA-Code")] string mfaCode,
            [FromHeader(Name = "X-Justification")] string justification)
        {
            var accessContext = this.CreateAccessContext("list_organizations", mfaCode, justification);
            var validation = await this._superAdminService.ValidateAccessAsync(accessContext);

            if (!validation.IsValid)
            {
                this._logger.LogWarning("Super admin access denied for user {UserId}: {Reason}", accessContext.UserId, validation.FailureReason);
                return this.StatusCode(
                    403,
                    new
                    {
                        error = validation.FailureReason,
                    });
            }

            var organizations = await this._superAdminService.GetCrossRegionOrganizationsAsync();

            return this.Ok(organizations);
        }

        /// <summary>
        /// Generate impersonation token for tenant access.
        /// </summary>
        /// <remarks>
        /// CRITICAL: This allows impersonating any user. Requires approval and justification.
        /// All impersonation attempts are permanently logged.
        /// </remarks>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPost("impersonate")]
        [ProducesResponseType(typeof(ImpersonationToken), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GenerateImpersonationToken(
            [FromBody] ImpersonationRequest request,
            [FromHeader(Name = "X-MFA-Code")] string mfaCode)
        {
            if (request == null)
            {
                return this.BadRequest(new { error = "Request body is required" });
            }

            var accessContext = this.CreateAccessContext("impersonate", mfaCode, request.Justification);
            var validation = await this._superAdminService.ValidateAccessAsync(accessContext);

            if (!validation.IsValid)
            {
                this._logger.LogWarning("Super admin impersonation denied for user {UserId}: {Reason}", accessContext.UserId, validation.FailureReason);
                return this.StatusCode(
                    403,
                    new
                    {
                        error = validation.FailureReason,
                    });
            }

            try
            {
                var token = await this._superAdminService.GenerateImpersonationTokenAsync(request);

                this._logger.LogWarning("⚠️  IMPERSONATION TOKEN GENERATED - User: {UserId}, Target: {TargetUserId}, Org: {OrgId}", this.GetCurrentUserId(), request.UserId, request.OrganizationId);

                return this.Ok(token);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Failed to generate impersonation token");
                return this.BadRequest(
                    new
                    {
                        error = ex.Message,
                    });
            }
        }

        /// <summary>
        /// Get global usage analytics across all regions.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpGet("analytics/usage")]
        [ProducesResponseType(typeof(GlobalUsageAnalytics), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetGlobalUsageAnalytics(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromHeader(Name = "X-MFA-Code")] string mfaCode)
        {
            var accessContext = this.CreateAccessContext("view_analytics", mfaCode, null);
            var validation = await this._superAdminService.ValidateAccessAsync(accessContext);

            if (!validation.IsValid)
            {
                return this.StatusCode(403, new { error = validation.FailureReason });
            }

            var analytics = await this._superAdminService.GetGlobalUsageAnalyticsAsync(startDate, endDate);

            return this.Ok(analytics);
        }

        /// <summary>
        /// Get cross-border transfer reports.
        /// </summary>
        /// <remarks>
        /// For GDPR/LGPD compliance auditing.
        /// </remarks>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpGet("compliance/cross-border-transfers")]
        [ProducesResponseType(typeof(List<CrossBorderTransferReport>), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetCrossBorderTransfers(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromHeader(Name = "X-MFA-Code")] string mfaCode)
        {
            var accessContext = this.CreateAccessContext("view_cross_border_transfers", mfaCode, null);
            var validation = await this._superAdminService.ValidateAccessAsync(accessContext);

            if (!validation.IsValid)
            {
                return this.StatusCode(403, new { error = validation.FailureReason });
            }

            var transfers = await this._superAdminService.GetCrossBorderTransfersAsync(startDate, endDate);

            return this.Ok(transfers);
        }

        /// <summary>
        /// Get compliance status dashboard.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpGet("compliance/status")]
        [ProducesResponseType(typeof(ComplianceStatusDashboard), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetComplianceStatus(
            [FromHeader(Name = "X-MFA-Code")] string mfaCode)
        {
            var accessContext = this.CreateAccessContext("view_compliance", mfaCode, null);
            var validation = await this._superAdminService.ValidateAccessAsync(accessContext);

            if (!validation.IsValid)
            {
                return this.StatusCode(403, new { error = validation.FailureReason });
            }

            var status = await this._superAdminService.GetComplianceStatusAsync();

            return this.Ok(status);
        }

        /// <summary>
        /// Get system health overview across regions.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpGet("system/health")]
        [ProducesResponseType(typeof(SystemHealthOverview), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetSystemHealth(
            [FromHeader(Name = "X-MFA-Code")] string mfaCode)
        {
            var accessContext = this.CreateAccessContext("view_system_health", mfaCode, null);
            var validation = await this._superAdminService.ValidateAccessAsync(accessContext);

            if (!validation.IsValid)
            {
                return this.StatusCode(403, new { error = validation.FailureReason });
            }

            var health = await this._superAdminService.GetSystemHealthOverviewAsync();

            return this.Ok(health);
        }

        /// <summary>
        /// Modify organization limits.
        /// </summary>
        /// <remarks>
        /// CRITICAL: Requires approval and justification. All modifications are audited.
        /// </remarks>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPost("organizations/{organizationId:guid}/limits")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> ModifyOrganizationLimits(
            Guid organizationId,
            [FromBody] LimitModificationRequest request,
            [FromHeader(Name = "X-MFA-Code")] string mfaCode)
        {
            if (request == null)
            {
                return this.BadRequest(new { error = "Request body is required" });
            }

            request.OrganizationId = organizationId;

            var accessContext = this.CreateAccessContext("modify_limits", mfaCode, request.Justification);
            var validation = await this._superAdminService.ValidateAccessAsync(accessContext);

            if (!validation.IsValid)
            {
                this._logger.LogWarning("Super admin limit modification denied for user {UserId}: {Reason}", accessContext.UserId, validation.FailureReason);
                return this.StatusCode(403, new { error = validation.FailureReason });
            }

            try
            {
                var success = await this._superAdminService.ModifyOrganizationLimitsAsync(request);

                this._logger.LogWarning(
                    "⚠️  LIMITS MODIFIED - Org: {OrgId}, Type: {Type}, Value: {Value}, By: {UserId}",
                    organizationId,
                    request.LimitType,
                    request.NewValue,
                    this.GetCurrentUserId());

                return this.Ok(
                    new
                    {
                        success,
                        message = "Limits modified successfully",
                    });
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Failed to modify organization limits");
                return this.BadRequest(
                    new
                    {
                        error = ex.Message,
                    });
            }
        }

        /// <summary>
        /// Validate super admin access.
        /// </summary>
        /// <remarks>
        /// Used by frontend to verify access before showing sensitive UI.
        /// </remarks>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPost("validate-access")]
        [ProducesResponseType(typeof(SuperAdminAccessValidation), 200)]
        public async Task<IActionResult> ValidateAccess(
            [FromBody] SuperAdminAccessContext context,
            [FromHeader(Name = "X-MFA-Code")] string mfaCode)
        {
            if (context == null)
            {
                return this.BadRequest(new { error = "Request body is required" });
            }

            context.UserId = this.GetCurrentUserId();
            context.IpAddress = GetClientIpAddress(this.HttpContext);
            context.MfaCode = mfaCode;
            context.RequestTime = DateTime.UtcNow;

            var validation = await this._superAdminService.ValidateAccessAsync(context);

            return this.Ok(validation);
        }

        // Private helper methods
        private SuperAdminAccessContext CreateAccessContext(string action, string mfaCode, string justification)
        {
            return new SuperAdminAccessContext
            {
                UserId = this.GetCurrentUserId(),
                IpAddress = GetClientIpAddress(this.HttpContext),
                MfaCode = mfaCode,
                Action = action,
                Justification = justification,
                RequestTime = DateTime.UtcNow,
            };
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? this.User.FindFirst("sub")?.Value;

            if (Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            throw new UnauthorizedAccessException("User ID not found in claims");
        }

        private static string GetClientIpAddress(Microsoft.AspNetCore.Http.HttpContext context)
        {
#pragma warning disable S6932 // Accessing headers in helper method is acceptable for IP address resolution
            // Try X-Forwarded-For first (for proxied requests)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                var ips = forwardedFor.Split(',');
                return ips[0].Trim();
            }

            // Fall back to direct connection IP
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
#pragma warning restore S6932 // Accessing headers in helper method is acceptable for IP address resolution
        }
    }
}
