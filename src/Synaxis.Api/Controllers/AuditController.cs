// <copyright file="AuditController.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.Controllers;

using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Synaxis.Core.Contracts;
using Synaxis.Infrastructure.Services.Audit;

/// <summary>
/// Controller for audit log operations.
/// Requires Admin or Auditor role for access.
/// </summary>
[ApiController]
[Route("api/v1/audit")]
[Authorize]
public class AuditController : ControllerBase
{
    private readonly IAuditQueryService _queryService;
    private readonly IAuditExportService _exportService;
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditController"/> class.
    /// </summary>
    /// <param name="queryService">The audit query service.</param>
    /// <param name="exportService">The audit export service.</param>
    /// <param name="auditService">The audit service for integrity verification.</param>
    /// <param name="logger">The logger.</param>
    public AuditController(
        IAuditQueryService queryService,
        IAuditExportService exportService,
        IAuditService auditService,
        ILogger<AuditController> logger)
    {
        ArgumentNullException.ThrowIfNull(queryService);
        ArgumentNullException.ThrowIfNull(exportService);
        ArgumentNullException.ThrowIfNull(auditService);
        ArgumentNullException.ThrowIfNull(logger);
        _queryService = queryService;
        _exportService = exportService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Queries audit logs with pagination and filtering.
    /// </summary>
    /// <param name="organizationId">The organization identifier.</param>
    /// <param name="userId">Optional user identifier filter.</param>
    /// <param name="searchTerm">Optional full-text search term.</param>
    /// <param name="eventType">Optional event type filter.</param>
    /// <param name="eventCategory">Optional event category filter.</param>
    /// <param name="fromDate">Optional start date filter.</param>
    /// <param name="toDate">Optional end date filter.</param>
    /// <param name="page">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paged result of audit logs.</returns>
    [HttpGet("logs")]
    [ProducesResponseType(typeof(Core.Models.PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> QueryLogs(
        [FromQuery] Guid organizationId,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? eventType = null,
        [FromQuery] string? eventCategory = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var authorizationResult = await AuthorizeAccessAsync(organizationId);
        if (authorizationResult != null)
        {
            return authorizationResult;
        }

        if (organizationId == Guid.Empty)
        {
            return BadRequest(new { message = "OrganizationId is required" });
        }

        var request = new AuditQueryRequest(
            OrganizationId: organizationId,
            UserId: userId,
            SearchTerm: searchTerm,
            EventType: eventType,
            EventCategory: eventCategory,
            FromDate: fromDate,
            ToDate: toDate,
            Page: page,
            PageSize: pageSize);

        if (!request.IsValid())
        {
            return BadRequest(new { message = "Invalid query parameters" });
        }

        var result = await _queryService.QueryLogsAsync(request, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>
    /// Gets a specific audit log by its identifier.
    /// </summary>
    /// <param name="id">The audit log identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The audit log if found.</returns>
    [HttpGet("logs/{id:guid}")]
    [ProducesResponseType(typeof(AuditLogDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetLog(Guid id, CancellationToken cancellationToken = default)
    {
        var log = await _queryService.GetLogByIdAsync(id, cancellationToken).ConfigureAwait(false);

        if (log is null)
        {
            return NotFound(new { message = $"Audit log {id} not found" });
        }

        var authorizationResult = await AuthorizeAccessAsync(log.OrganizationId);
        if (authorizationResult != null)
        {
            return authorizationResult;
        }

        return Ok(log);
    }

    /// <summary>
    /// Gets audit statistics for an organization.
    /// </summary>
    /// <param name="organizationId">The organization identifier.</param>
    /// <param name="from">Optional start date filter.</param>
    /// <param name="to">Optional end date filter.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Aggregated statistics for audit logs.</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(AuditStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStatistics(
        [FromQuery] Guid organizationId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var authorizationResult = await AuthorizeAccessAsync(organizationId);
        if (authorizationResult != null)
        {
            return authorizationResult;
        }

        if (organizationId == Guid.Empty)
        {
            return BadRequest(new { message = "OrganizationId is required" });
        }

        var statistics = await _queryService.GetStatisticsAsync(organizationId, from, to, cancellationToken).ConfigureAwait(false);
        return Ok(statistics);
    }

    /// <summary>
    /// Exports audit logs to JSON format.
    /// </summary>
    /// <param name="request">The export request parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The export result with file path and metadata.</returns>
    [HttpPost("export/json")]
    [ProducesResponseType(typeof(ExportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportToJson([FromBody] AuditExportRequest request, CancellationToken cancellationToken = default)
    {
        var authorizationResult = await AuthorizeAccessAsync(request.OrganizationId);
        if (authorizationResult != null)
        {
            return authorizationResult;
        }

        if (!request.IsValid())
        {
            return BadRequest(new { message = "Invalid export request parameters" });
        }

        var result = await _exportService.ExportToJsonAsync(request, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Exported {RecordCount} audit logs to JSON for organization {OrganizationId}",
            result.RecordCount,
            request.OrganizationId);

        return Ok(result);
    }

    /// <summary>
    /// Exports audit logs to CSV format.
    /// </summary>
    /// <param name="request">The export request parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The export result with file path and metadata.</returns>
    [HttpPost("export/csv")]
    [ProducesResponseType(typeof(ExportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportToCsv([FromBody] AuditExportRequest request, CancellationToken cancellationToken = default)
    {
        var authorizationResult = await AuthorizeAccessAsync(request.OrganizationId);
        if (authorizationResult != null)
        {
            return authorizationResult;
        }

        if (!request.IsValid())
        {
            return BadRequest(new { message = "Invalid export request parameters" });
        }

        var result = await _exportService.ExportToCsvAsync(request, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Exported {RecordCount} audit logs to CSV for organization {OrganizationId}",
            result.RecordCount,
            request.OrganizationId);

        return Ok(result);
    }

    /// <summary>
    /// Verifies the integrity of an audit log entry.
    /// </summary>
    /// <param name="id">The audit log identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The integrity verification result.</returns>
    [HttpGet("integrity/{id:guid}")]
    [ProducesResponseType(typeof(IntegrityVerificationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> VerifyIntegrity(Guid id, CancellationToken cancellationToken = default)
    {
        var log = await _queryService.GetLogByIdAsync(id, cancellationToken).ConfigureAwait(false);

        if (log is null)
        {
            return NotFound(new { message = $"Audit log {id} not found" });
        }

        var authorizationResult = await AuthorizeAccessAsync(log.OrganizationId);
        if (authorizationResult != null)
        {
            return authorizationResult;
        }

        var isValid = await _auditService.VerifyIntegrityAsync(id).ConfigureAwait(false);

        var result = new IntegrityVerificationResult(
            LogId: id,
            IsValid: isValid,
            VerifiedAt: DateTime.UtcNow,
            Message: isValid ? "Audit log integrity verified successfully" : "Audit log integrity check failed - possible tampering detected");

        if (!isValid)
        {
            _logger.LogWarning("Integrity verification failed for audit log {LogId}", id);
        }

        return Ok(result);
    }

    /// <summary>
    /// Authorizes access to audit data for a specific organization.
    /// </summary>
    /// <param name="organizationId">The organization identifier.</param>
    /// <returns>An IActionResult if unauthorized; otherwise, null.</returns>
    private Task<IActionResult?> AuthorizeAccessAsync(Guid organizationId)
    {
        // Check if user is authenticated
        if (User.Identity?.IsAuthenticated != true)
        {
            return Task.FromResult<IActionResult?>(Unauthorized(new { message = "Authentication required" }));
        }

        // Check for Admin or Auditor role
        var isAdmin = User.IsInRole("admin") || User.IsInRole("Admin");
        var isAuditor = User.IsInRole("auditor") || User.IsInRole("Auditor");

        if (!isAdmin && !isAuditor)
        {
            return Task.FromResult<IActionResult?>(Forbid());
        }

        // If not admin, check organization access
        if (!isAdmin)
        {
            var userOrgClaim = User.FindFirst("organization_id")?.Value;
            if (string.IsNullOrEmpty(userOrgClaim) || !Guid.TryParse(userOrgClaim, out var userOrgId))
            {
                return Task.FromResult<IActionResult?>(Forbid());
            }

            if (userOrgId != organizationId)
            {
                _logger.LogWarning(
                    "User attempted to access organization {RequestedOrgId} but belongs to {UserOrgId}",
                    organizationId,
                    userOrgId);
                return Task.FromResult<IActionResult?>(Forbid());
            }
        }

        return Task.FromResult<IActionResult?>(null);
    }
}

/// <summary>
/// Result of an integrity verification operation.
/// </summary>
/// <param name="LogId">The audit log identifier.</param>
/// <param name="IsValid">Whether the audit log integrity is valid.</param>
/// <param name="VerifiedAt">When the verification was performed.</param>
/// <param name="Message">A descriptive message about the verification result.</param>
public record IntegrityVerificationResult(
    Guid LogId,
    bool IsValid,
    DateTime VerifiedAt,
    string Message);
