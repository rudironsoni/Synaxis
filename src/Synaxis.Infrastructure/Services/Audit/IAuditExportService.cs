// <copyright file="IAuditExportService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services.Audit;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Service for exporting audit logs in various formats.
/// </summary>
public interface IAuditExportService
{
    /// <summary>
    /// Exports audit logs to JSON format.
    /// </summary>
    /// <param name="request">The export request parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The export result with file path and metadata.</returns>
    Task<ExportResult> ExportToJsonAsync(AuditExportRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports audit logs to CSV format.
    /// </summary>
    /// <param name="request">The export request parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The export result with file path and metadata.</returns>
    Task<ExportResult> ExportToCsvAsync(AuditExportRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a PDF report from audit logs.
    /// </summary>
    /// <param name="request">The report request parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The PDF file content as bytes.</returns>
    Task<byte[]> GenerateReportAsync(AuditReportRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Request parameters for exporting audit logs.
/// </summary>
/// <param name="OrganizationId">The organization identifier.</param>
/// <param name="FromDate">The start date for the export range.</param>
/// <param name="ToDate">The end date for the export range.</param>
/// <param name="Format">The export format (json, csv).</param>
public record AuditExportRequest(
    Guid OrganizationId,
    DateTime FromDate,
    DateTime ToDate,
    string Format = "json")
{
    /// <summary>
    /// Validates the export request.
    /// </summary>
    /// <returns>True if valid; otherwise, false.</returns>
    public bool IsValid()
    {
        if (this.OrganizationId == Guid.Empty)
        {
            return false;
        }

        if (this.FromDate > this.ToDate)
        {
            return false;
        }

        return true;
    }
}

/// <summary>
/// Result of an export operation.
/// </summary>
/// <param name="FilePath">The path to the exported file.</param>
/// <param name="RecordCount">The number of records exported.</param>
/// <param name="FileSizeBytes">The size of the exported file in bytes.</param>
public record ExportResult(
    string FilePath,
    int RecordCount,
    long FileSizeBytes);

/// <summary>
/// Request parameters for generating an audit report.
/// </summary>
/// <param name="OrganizationId">The organization identifier.</param>
/// <param name="FromDate">The start date for the report range.</param>
/// <param name="ToDate">The end date for the report range.</param>
/// <param name="ReportType">The type of report (summary, detailed).</param>
public record AuditReportRequest(
    Guid OrganizationId,
    DateTime FromDate,
    DateTime ToDate,
    string ReportType = "summary")
{
    /// <summary>
    /// Validates the report request.
    /// </summary>
    /// <returns>True if valid; otherwise, false.</returns>
    public bool IsValid()
    {
        if (this.OrganizationId == Guid.Empty)
        {
            return false;
        }

        if (this.FromDate > this.ToDate)
        {
            return false;
        }

        return true;
    }
}
