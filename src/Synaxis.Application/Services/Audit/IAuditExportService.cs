// <copyright file="IAuditExportService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Application.Services.Audit;

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
    /// Generates a report from audit logs.
    /// </summary>
    /// <param name="request">The report request parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The report file content as bytes.</returns>
    Task<byte[]> GenerateReportAsync(AuditReportRequest request, CancellationToken cancellationToken = default);
}
