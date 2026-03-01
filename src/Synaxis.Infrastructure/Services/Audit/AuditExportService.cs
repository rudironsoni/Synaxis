// <copyright file="AuditExportService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services.Audit;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Synaxis.Core.Contracts;
using Synaxis.Core.Models;

/// <summary>
/// Service for exporting audit logs in various formats.
/// </summary>
public class AuditExportService : IAuditExportService
{
    private readonly IAuditLogRepository _repository;
    private readonly ILogger<AuditExportService> _logger;
    private readonly string _exportDirectory;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditExportService"/> class.
    /// </summary>
    /// <param name="repository">The audit log repository.</param>
    /// <param name="logger">The logger.</param>
    public AuditExportService(IAuditLogRepository repository, ILogger<AuditExportService> logger)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(logger);
        this._repository = repository;
        this._logger = logger;
        this._exportDirectory = Path.Combine(Path.GetTempPath(), "synaxis-audit-exports");

        Directory.CreateDirectory(this._exportDirectory);
    }

    /// <inheritdoc/>
    public async Task<ExportResult> ExportToJsonAsync(AuditExportRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.IsValid())
        {
            throw new ArgumentException("Invalid export request parameters", nameof(request));
        }

        var logs = await this.GetLogsForExportAsync(request, cancellationToken).ConfigureAwait(false);
        var fileName = $"audit-export-{request.OrganizationId}-{DateTime.UtcNow:yyyyMMddHHmmss}.json";
        var filePath = Path.Combine(this._exportDirectory, fileName);

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        var jsonContent = JsonSerializer.Serialize(logs, jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(jsonContent);

        await File.WriteAllBytesAsync(filePath, bytes, cancellationToken).ConfigureAwait(false);

        this._logger.LogInformation(
            "Exported {Count} audit logs to JSON for organization {OrganizationId}",
            logs.Count,
            request.OrganizationId);

        return new ExportResult(filePath, logs.Count, bytes.Length);
    }

    /// <inheritdoc/>
    public async Task<ExportResult> ExportToCsvAsync(AuditExportRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.IsValid())
        {
            throw new ArgumentException("Invalid export request parameters", nameof(request));
        }

        var logs = await this.GetLogsForExportAsync(request, cancellationToken).ConfigureAwait(false);
        var fileName = $"audit-export-{request.OrganizationId}-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
        var filePath = Path.Combine(this._exportDirectory, fileName);

        var csvContent = this.BuildCsvContent(logs);
        var bytes = Encoding.UTF8.GetBytes(csvContent);

        await File.WriteAllBytesAsync(filePath, bytes, cancellationToken).ConfigureAwait(false);

        this._logger.LogInformation(
            "Exported {Count} audit logs to CSV for organization {OrganizationId}",
            logs.Count,
            request.OrganizationId);

        return new ExportResult(filePath, logs.Count, bytes.Length);
    }

    /// <inheritdoc/>
    public Task<byte[]> GenerateReportAsync(AuditReportRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.IsValid())
        {
            throw new ArgumentException("Invalid report request parameters", nameof(request));
        }

        // PDF generation would require additional libraries like iTextSharp or QuestPDF
        // For now, throw NotSupportedException as specified in the task
        throw new NotSupportedException("PDF report generation is not yet implemented. Use JSON or CSV export instead.");
    }

    /// <summary>
    /// Retrieves audit logs for export based on the request parameters.
    /// </summary>
    /// <param name="request">The export request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of audit logs.</returns>
    private async Task<IReadOnlyList<AuditLog>> GetLogsForExportAsync(AuditExportRequest request, CancellationToken cancellationToken)
    {
        var criteria = new AuditSearchCriteria(
            OrganizationId: request.OrganizationId,
            FromDate: request.FromDate,
            ToDate: request.ToDate,
            PageSize: int.MaxValue);

        var result = await this._repository.SearchAsync(criteria, cancellationToken).ConfigureAwait(false);
        return result.Items;
    }

    /// <summary>
    /// Builds CSV content from audit logs.
    /// </summary>
    /// <param name="logs">The audit logs.</param>
    /// <returns>CSV formatted string.</returns>
    private string BuildCsvContent(IReadOnlyList<AuditLog> logs)
    {
        var sb = new StringBuilder();

        // Header row
        sb.AppendLine("Id,OrganizationId,UserId,EventType,EventCategory,Action,ResourceType,ResourceId,IpAddress,UserAgent,Region,Timestamp");

        // Data rows
        foreach (var log in logs)
        {
            sb.AppendLine($"\"{log.Id}\",\"{log.OrganizationId}\",\"{log.UserId}\",\"{EscapeCsvField(log.EventType)}\",\"{EscapeCsvField(log.EventCategory)}\",\"{EscapeCsvField(log.Action)}\",\"{EscapeCsvField(log.ResourceType)}\",\"{EscapeCsvField(log.ResourceId)}\",\"{EscapeCsvField(log.IpAddress)}\",\"{EscapeCsvField(log.UserAgent)}\",\"{EscapeCsvField(log.Region)}\",\"{log.Timestamp:O}\"");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Escapes a field for CSV format.
    /// </summary>
    /// <param name="field">The field to escape.</param>
    /// <returns>The escaped field.</returns>
    private static string EscapeCsvField(string? field)
    {
        if (string.IsNullOrEmpty(field))
        {
            return string.Empty;
        }

        return field.Replace("\"", "\"\"", StringComparison.Ordinal);
    }
}
