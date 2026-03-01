// <copyright file="UsageReportsController.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Api.Controllers;

using Billing.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Controller for usage reporting and analytics endpoints.
/// </summary>
[ApiController]
[Route("api/v1/usage")]
[Authorize]
public class UsageReportsController : ControllerBase
{
    private readonly IUsageReportingService _usageService;

    /// <summary>
    /// Initializes a new instance of the <see cref="UsageReportsController"/> class.
    /// </summary>
    public UsageReportsController(IUsageReportingService usageService)
    {
        _usageService = usageService ?? throw new ArgumentNullException(nameof(usageService));
    }

    /// <summary>
    /// Gets a detailed usage report for the current organization.
    /// </summary>
    [HttpGet("report")]
    [ProducesResponseType(typeof(UsageReportDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsageReport([FromQuery] UsageReportRequest request, CancellationToken cancellationToken)
    {
        var organizationId = GetOrganizationId();
        var reportRequest = request with { OrganizationId = organizationId };
        var report = await _usageService.GetUsageReportAsync(reportRequest, cancellationToken);
        return Ok(report);
    }

    /// <summary>
    /// Gets usage aggregated by resource type.
    /// </summary>
    [HttpGet("by-resource")]
    [ProducesResponseType(typeof(IReadOnlyList<UsageByResourceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsageByResource(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken cancellationToken)
    {
        var organizationId = GetOrganizationId();
        var usage = await _usageService.GetUsageByResourceAsync(organizationId, from, to, cancellationToken);
        return Ok(usage);
    }

    /// <summary>
    /// Gets usage over time for a specific resource type.
    /// </summary>
    [HttpGet("over-time")]
    [ProducesResponseType(typeof(IReadOnlyList<UsageByTimeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsageOverTime(
        [FromQuery] string resourceType,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken cancellationToken)
    {
        var organizationId = GetOrganizationId();
        var usage = await _usageService.GetUsageOverTimeAsync(organizationId, resourceType, from, to, cancellationToken);
        return Ok(usage);
    }

    /// <summary>
    /// Gets a summary of current and previous month usage.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(UsageSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsageSummary(CancellationToken cancellationToken)
    {
        var organizationId = GetOrganizationId();
        var summary = await _usageService.GetUsageSummaryAsync(organizationId, cancellationToken);
        return Ok(summary);
    }

    /// <summary>
    /// Exports usage data in the specified format.
    /// </summary>
    [HttpGet("export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportUsageReport(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] string format = "csv",
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetOrganizationId();
        var request = new UsageExportRequest(organizationId, from, to, format);
        var data = await _usageService.ExportUsageReportAsync(request, cancellationToken);

        var contentType = format.ToLowerInvariant() switch
        {
            "json" => "application/json",
            _ => "text/csv"
        };

        var fileName = $"usage-report-{from:yyyyMMdd}-{to:yyyyMMdd}.{format}";

        return File(data, contentType, fileName);
    }

    private Guid GetOrganizationId()
    {
        var claim = User.FindFirst("organization_id")?.Value;
        return claim != null ? Guid.Parse(claim) : Guid.Empty;
    }
}
