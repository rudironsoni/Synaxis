// <copyright file="AuditQueryService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services.Audit;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Synaxis.Core.Contracts;
using Synaxis.Core.Models;

/// <summary>
/// Service for querying audit logs with pagination and filtering.
/// </summary>
public class AuditQueryService : IAuditQueryService
{
    private readonly IAuditLogRepository _repository;
    private readonly ILogger<AuditQueryService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditQueryService"/> class.
    /// </summary>
    /// <param name="repository">The audit log repository.</param>
    /// <param name="logger">The logger.</param>
    public AuditQueryService(IAuditLogRepository repository, ILogger<AuditQueryService> logger)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(logger);
        this._repository = repository;
        this._logger = logger;
    }

    /// <inheritdoc/>
    public async Task<PagedResult<AuditLogDto>> QueryLogsAsync(AuditQueryRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.IsValid())
        {
            throw new ArgumentException("Invalid query request parameters", nameof(request));
        }

        var criteria = new AuditSearchCriteria(
            OrganizationId: request.OrganizationId,
            UserId: request.UserId,
            SearchTerm: request.SearchTerm,
            EventType: request.EventType,
            EventCategory: request.EventCategory,
            FromDate: request.FromDate,
            ToDate: request.ToDate,
            Page: request.Page,
            PageSize: request.PageSize);

        var result = await this._repository.SearchAsync(criteria, cancellationToken).ConfigureAwait(false);

        var dtos = result.Items.Select(AuditQueryService.MapToDto).ToList();

        this._logger.LogDebug(
            "Query returned {Count} of {Total} audit logs for organization {OrganizationId}",
            dtos.Count,
            result.TotalCount,
            request.OrganizationId);

        return new PagedResult<AuditLogDto>(dtos, result.TotalCount, result.Page, result.PageSize);
    }

    /// <inheritdoc/>
    public async Task<AuditLogDto?> GetLogByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Log ID cannot be empty", nameof(id));
        }

        var log = await this._repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);

        return log is null ? null : AuditQueryService.MapToDto(log);
    }

    /// <inheritdoc/>
    public async Task<AuditStatisticsDto> GetStatisticsAsync(
        Guid organizationId,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("Organization ID cannot be empty", nameof(organizationId));
        }

        var criteria = new AuditSearchCriteria(
            OrganizationId: organizationId,
            FromDate: from,
            ToDate: to,
            PageSize: int.MaxValue);

        var result = await this._repository.SearchAsync(criteria, cancellationToken).ConfigureAwait(false);
        var logs = result.Items;

        var statistics = new AuditStatisticsDto(
            TotalEvents: logs.Count,
            EventsByType: logs
                .GroupBy(l => l.EventType, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal),
            EventsByCategory: logs
                .GroupBy(l => l.EventCategory, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal),
            EventsOverTime: logs
                .GroupBy(l => l.Timestamp.Date)
                .ToDictionary(g => g.Key, g => g.Count()));

        this._logger.LogDebug(
            "Generated statistics for {Count} audit logs in organization {OrganizationId}",
            logs.Count,
            organizationId);

        return statistics;
    }

    /// <summary>
    /// Maps an AuditLog entity to an AuditLogDto.
    /// </summary>
    /// <param name="log">The audit log entity.</param>
    /// <returns>The DTO.</returns>
    private static AuditLogDto MapToDto(AuditLog log)
    {
        return new AuditLogDto(
            Id: log.Id,
            OrganizationId: log.OrganizationId,
            UserId: log.UserId,
            EventType: log.EventType,
            EventCategory: log.EventCategory,
            Action: log.Action,
            ResourceType: log.ResourceType,
            ResourceId: string.IsNullOrEmpty(log.ResourceId) ? null : log.ResourceId,
            Metadata: log.Metadata.Count > 0 ? new Dictionary<string, object>(log.Metadata, StringComparer.Ordinal) : null,
            IpAddress: string.IsNullOrEmpty(log.IpAddress) ? null : log.IpAddress,
            UserAgent: string.IsNullOrEmpty(log.UserAgent) ? null : log.UserAgent,
            Region: string.IsNullOrEmpty(log.Region) ? null : log.Region,
            Timestamp: log.Timestamp);
    }
}
