// <copyright file="AuditExportService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Application.Services.Audit;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Synaxis.Core.Contracts;
using Synaxis.Core.Models;

/// <summary>
/// Implementation of audit export service.
/// </summary>
public class AuditExportService : IAuditExportService
{
    private readonly IAuditLogRepository _auditLogRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditExportService"/> class.
    /// </summary>
    /// <param name="auditLogRepository">The audit log repository.</param>
    public AuditExportService(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
    }

    /// <inheritdoc/>
    public async Task<ExportResult> ExportToJsonAsync(AuditExportRequest request, CancellationToken cancellationToken = default)
    {
        // Guard clauses for input validation
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.OrganizationId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId must be provided.", nameof(request));
        }

        if (request.FromDate > request.ToDate)
        {
            throw new ArgumentException("FromDate must be less than or equal to ToDate.", nameof(request));
        }

        // Get all audit logs in the specified date range
        var criteria = new AuditSearchCriteria(
            OrganizationId: request.OrganizationId,
            FromDate: request.FromDate,
            ToDate: request.ToDate,
            Page: 1,
            PageSize: int.MaxValue,
        );

        var result = await this._auditLogRepository.SearchAsync(criteria, cancellationToken).ConfigureAwait(false);

        // Convert to JSON
        var json = JsonSerializer.Serialize(result.Items, new JsonSerializerOptions
        {
            WriteIndented = true,
        });

        // Save to file
        var fileName = $"audit_export_{request.OrganizationId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
        var filePath = Path.Combine(Path.GetTempPath(), fileName);

        await File.WriteAllTextAsync(filePath, json, cancellationToken).ConfigureAwait(false);

        return new ExportResult(
            FilePath: filePath,
            RecordCount: result.Items.Count,
            FileSizeBytes: new FileInfo(filePath).Length);
    }

    /// <inheritdoc/>
    public async Task<ExportResult> ExportToCsvAsync(AuditExportRequest request, CancellationToken cancellationToken = default)
    {
        // Guard clauses for input validation
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.OrganizationId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId must be provided.", nameof(request));
        }

        if (request.FromDate > request.ToDate)
        {
            throw new ArgumentException("FromDate must be less than or equal to ToDate.", nameof(request));
        }

        // Get all audit logs in the specified date range
        var criteria = new AuditSearchCriteria(
            OrganizationId: request.OrganizationId,
            FromDate: request.FromDate,
            ToDate: request.ToDate,
            Page: 1,
            PageSize: int.MaxValue,
        );

        var result = await this._auditLogRepository.SearchAsync(criteria, cancellationToken).ConfigureAwait(false);

        // Convert to CSV
        var csv = new StringBuilder();
        
        // Add header row
        csv.AppendLine("Id,OrganizationId,UserId,EventType,EventCategory,Action,ResourceType,ResourceId,Timestamp,IpAddress,UserAgent,Region");

        // Add data rows
        foreach (var log in result.Items)
        {
            csv.AppendLine(CultureInfo.InvariantCulture, $"{log.Id},{log.OrganizationId},{log.UserId?.ToString() ?? string.Empty}," +
                          $"{EscapeCsvField(log.EventType)},{EscapeCsvField(log.EventCategory)},{EscapeCsvField(log.Action)}," +
                          $"{EscapeCsvField(log.ResourceType)},{EscapeCsvField(log.ResourceId ?? string.Empty)},{log.Timestamp:yyyy-MM-dd HH:mm:ss}," +
                          $"{EscapeCsvField(log.IpAddress ?? string.Empty)},{EscapeCsvField(log.UserAgent ?? string.Empty)},{EscapeCsvField(log.Region)}");
        }

        // Save to file
        var fileName = $"audit_export_{request.OrganizationId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        var filePath = Path.Combine(Path.GetTempPath(), fileName);

        await File.WriteAllTextAsync(filePath, csv.ToString(), cancellationToken).ConfigureAwait(false);

        return new ExportResult(
            FilePath: filePath,
            RecordCount: result.Items.Count,
            FileSizeBytes: new FileInfo(filePath).Length);
    }

    /// <inheritdoc/>
    public async Task<byte[]> GenerateReportAsync(AuditReportRequest request, CancellationToken cancellationToken = default)
    {
        // Guard clauses for input validation
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.OrganizationId == Guid.Empty)
        {
            throw new ArgumentException("OrganizationId must be provided.", nameof(request));
        }

        if (request.FromDate > request.ToDate)
        {
            throw new ArgumentException("FromDate must be less than or equal to ToDate.", nameof(request));
        }

        // For now, we'll generate a simple text report
        // In a real implementation, this could generate a PDF or other formatted report
        
        var criteria = new AuditSearchCriteria(
            OrganizationId: request.OrganizationId,
            FromDate: request.FromDate,
            ToDate: request.ToDate,
            Page: 1,
            PageSize: int.MaxValue,
        );

        var result = await this._auditLogRepository.SearchAsync(criteria, cancellationToken).ConfigureAwait(false);

        var report = new StringBuilder();
        report.AppendLine(CultureInfo.InvariantCulture, $"Audit Report for Organization: {request.OrganizationId}");
        report.AppendLine(CultureInfo.InvariantCulture, $"Period: {request.FromDate:yyyy-MM-dd} to {request.ToDate:yyyy-MM-dd}");
        report.AppendLine(CultureInfo.InvariantCulture, $"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        report.AppendLine();
        report.AppendLine(CultureInfo.InvariantCulture, $"Total Events: {result.TotalCount}");
        report.AppendLine();

        // Group by event type
        var eventsByType = result.Items
            .GroupBy(x => x.EventType, StringComparer.Ordinal)
            .OrderByDescending(g => g.Count())
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        report.AppendLine("Events by Type:");
        foreach (var kvp in eventsByType)
        {
            report.AppendLine(CultureInfo.InvariantCulture, $"  {kvp.Key}: {kvp.Value}");
        }

        report.AppendLine();

        // Group by event category
        var eventsByCategory = result.Items
            .GroupBy(x => x.EventCategory, StringComparer.Ordinal)
            .OrderByDescending(g => g.Count())
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        report.AppendLine("Events by Category:");
        foreach (var kvp in eventsByCategory)
        {
            report.AppendLine(CultureInfo.InvariantCulture, $"  {kvp.Key}: {kvp.Value}");
        }

        return Encoding.UTF8.GetBytes(report.ToString());
    }

    /// <summary>
    /// Escapes a field for CSV output.
    /// </summary>
    /// <param name="field">The field to escape.</param>
    /// <returns>The escaped field.</returns>
    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
        {
            return string.Empty;
        }

        // If the field contains commas, quotes, or newlines, wrap it in quotes
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            // Escape quotes by doubling them
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }
}
