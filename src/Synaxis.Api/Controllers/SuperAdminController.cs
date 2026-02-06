using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Synaxis.Core.Contracts;

namespace Synaxis.Api.Controllers
{
    /// <summary>
    /// Super Admin controller for cross-region visibility and management
    /// Requires SuperAdmin role, TOTP MFA, IP allowlist, and business hours access
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
            _superAdminService = superAdminService ?? throw new ArgumentNullException(nameof(superAdminService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        /// <summary>
        /// Get all organizations across all regions
        /// </summary>
        /// <remarks>
        /// WARNING: This endpoint exposes cross-region data. All access is audited.
        /// </remarks>
        [HttpGet("organizations")]
        [ProducesResponseType(typeof(List<OrganizationSummary>), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetCrossRegionOrganizations(
            [FromHeader(Name = "X-MFA-Code")] string mfaCode,
            [FromHeader(Name = "X-Justification")] string justification)
        {
            var accessContext = CreateAccessContext("list_organizations", mfaCode, justification);
            var validation = await _superAdminService.ValidateAccessAsync(accessContext);
            
            if (!validation.IsValid)
            {
                _logger.LogWarning("Super admin access denied for user {UserId}: {Reason}",
                    accessContext.UserId, validation.FailureReason);
                return StatusCode(403, new { error = validation.FailureReason });
            }
            
            var organizations = await _superAdminService.GetCrossRegionOrganizationsAsync();
            
            return Ok(organizations);
        }
        
        /// <summary>
        /// Generate impersonation token for tenant access
        /// </summary>
        /// <remarks>
        /// CRITICAL: This allows impersonating any user. Requires approval and justification.
        /// All impersonation attempts are permanently logged.
        /// </remarks>
        [HttpPost("impersonate")]
        [ProducesResponseType(typeof(ImpersonationToken), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GenerateImpersonationToken(
            [FromBody] ImpersonationRequest request,
            [FromHeader(Name = "X-MFA-Code")] string mfaCode)
        {
            if (request == null)
                return BadRequest(new { error = "Request body is required" });
            
            var accessContext = CreateAccessContext("impersonate", mfaCode, request.Justification);
            var validation = await _superAdminService.ValidateAccessAsync(accessContext);
            
            if (!validation.IsValid)
            {
                _logger.LogWarning("Super admin impersonation denied for user {UserId}: {Reason}",
                    accessContext.UserId, validation.FailureReason);
                return StatusCode(403, new { error = validation.FailureReason });
            }
            
            try
            {
                var token = await _superAdminService.GenerateImpersonationTokenAsync(request);
                
                _logger.LogWarning("⚠️  IMPERSONATION TOKEN GENERATED - User: {UserId}, Target: {TargetUserId}, Org: {OrgId}",
                    GetCurrentUserId(), request.UserId, request.OrganizationId);
                
                return Ok(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate impersonation token");
                return BadRequest(new { error = ex.Message });
            }
        }
        
        /// <summary>
        /// Get global usage analytics across all regions
        /// </summary>
        [HttpGet("analytics/usage")]
        [ProducesResponseType(typeof(GlobalUsageAnalytics), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetGlobalUsageAnalytics(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromHeader(Name = "X-MFA-Code")] string mfaCode)
        {
            var accessContext = CreateAccessContext("view_analytics", mfaCode, null);
            var validation = await _superAdminService.ValidateAccessAsync(accessContext);
            
            if (!validation.IsValid)
            {
                return StatusCode(403, new { error = validation.FailureReason });
            }
            
            var analytics = await _superAdminService.GetGlobalUsageAnalyticsAsync(startDate, endDate);
            
            return Ok(analytics);
        }
        
        /// <summary>
        /// Get cross-border transfer reports
        /// </summary>
        /// <remarks>
        /// For GDPR/LGPD compliance auditing
        /// </remarks>
        [HttpGet("compliance/cross-border-transfers")]
        [ProducesResponseType(typeof(List<CrossBorderTransferReport>), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetCrossBorderTransfers(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromHeader(Name = "X-MFA-Code")] string mfaCode)
        {
            var accessContext = CreateAccessContext("view_cross_border_transfers", mfaCode, null);
            var validation = await _superAdminService.ValidateAccessAsync(accessContext);
            
            if (!validation.IsValid)
            {
                return StatusCode(403, new { error = validation.FailureReason });
            }
            
            var transfers = await _superAdminService.GetCrossBorderTransfersAsync(startDate, endDate);
            
            return Ok(transfers);
        }
        
        /// <summary>
        /// Get compliance status dashboard
        /// </summary>
        [HttpGet("compliance/status")]
        [ProducesResponseType(typeof(ComplianceStatusDashboard), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetComplianceStatus(
            [FromHeader(Name = "X-MFA-Code")] string mfaCode)
        {
            var accessContext = CreateAccessContext("view_compliance", mfaCode, null);
            var validation = await _superAdminService.ValidateAccessAsync(accessContext);
            
            if (!validation.IsValid)
            {
                return StatusCode(403, new { error = validation.FailureReason });
            }
            
            var status = await _superAdminService.GetComplianceStatusAsync();
            
            return Ok(status);
        }
        
        /// <summary>
        /// Get system health overview across regions
        /// </summary>
        [HttpGet("system/health")]
        [ProducesResponseType(typeof(SystemHealthOverview), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetSystemHealth(
            [FromHeader(Name = "X-MFA-Code")] string mfaCode)
        {
            var accessContext = CreateAccessContext("view_system_health", mfaCode, null);
            var validation = await _superAdminService.ValidateAccessAsync(accessContext);
            
            if (!validation.IsValid)
            {
                return StatusCode(403, new { error = validation.FailureReason });
            }
            
            var health = await _superAdminService.GetSystemHealthOverviewAsync();
            
            return Ok(health);
        }
        
        /// <summary>
        /// Modify organization limits
        /// </summary>
        /// <remarks>
        /// CRITICAL: Requires approval and justification. All modifications are audited.
        /// </remarks>
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
                return BadRequest(new { error = "Request body is required" });
            
            request.OrganizationId = organizationId;
            
            var accessContext = CreateAccessContext("modify_limits", mfaCode, request.Justification);
            var validation = await _superAdminService.ValidateAccessAsync(accessContext);
            
            if (!validation.IsValid)
            {
                _logger.LogWarning("Super admin limit modification denied for user {UserId}: {Reason}",
                    accessContext.UserId, validation.FailureReason);
                return StatusCode(403, new { error = validation.FailureReason });
            }
            
            try
            {
                var success = await _superAdminService.ModifyOrganizationLimitsAsync(request);
                
                _logger.LogWarning("⚠️  LIMITS MODIFIED - Org: {OrgId}, Type: {Type}, Value: {Value}, By: {UserId}",
                    organizationId, request.LimitType, request.NewValue, GetCurrentUserId());
                
                return Ok(new { success, message = "Limits modified successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to modify organization limits");
                return BadRequest(new { error = ex.Message });
            }
        }
        
        /// <summary>
        /// Validate super admin access
        /// </summary>
        /// <remarks>
        /// Used by frontend to verify access before showing sensitive UI
        /// </remarks>
        [HttpPost("validate-access")]
        [ProducesResponseType(typeof(SuperAdminAccessValidation), 200)]
        public async Task<IActionResult> ValidateAccess(
            [FromBody] SuperAdminAccessContext context,
            [FromHeader(Name = "X-MFA-Code")] string mfaCode)
        {
            if (context == null)
                return BadRequest(new { error = "Request body is required" });
            
            context.UserId = GetCurrentUserId();
            context.IpAddress = GetClientIpAddress();
            context.MfaCode = mfaCode;
            context.RequestTime = DateTime.UtcNow;
            
            var validation = await _superAdminService.ValidateAccessAsync(context);
            
            return Ok(validation);
        }
        
        // Private helper methods
        
        private SuperAdminAccessContext CreateAccessContext(string action, string mfaCode, string justification)
        {
            return new SuperAdminAccessContext
            {
                UserId = GetCurrentUserId(),
                IpAddress = GetClientIpAddress(),
                MfaCode = mfaCode,
                Action = action,
                Justification = justification,
                RequestTime = DateTime.UtcNow
            };
        }
        
        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                           ?? User.FindFirst("sub")?.Value;
            
            if (Guid.TryParse(userIdClaim, out var userId))
                return userId;
            
            throw new UnauthorizedAccessException("User ID not found in claims");
        }
        
        private string GetClientIpAddress()
        {
            // Try X-Forwarded-For first (for proxied requests)
            var forwardedFor = Request.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                var ips = forwardedFor.Split(',');
                return ips[0].Trim();
            }
            
            // Fall back to direct connection IP
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }
}
