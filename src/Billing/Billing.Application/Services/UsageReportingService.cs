// <copyright file="UsageReportingService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Application.Services;

using System.Globalization;
using System.Text;
using System.Text.Json;
using Billing.Domain.Entities;
using Billing.Infrastructure.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

/// <summary>
/// Implementation of the usage reporting service for querying and aggregating usage data.
/// </summary>
public class UsageReportingService : IUsageReportingService
{
    private readonly IUsageRecordRepository _usageRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UsageReportingService> _logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Initializes a new instance of the <see cref="UsageReportingService"/> class.
    /// </summary>
    public UsageReportingService(
        IUsageRecordRepository usageRepository,
        IMemoryCache cache,
        ILogger<UsageReportingService> logger)
    {
        _usageRepository = usageRepository ?? throw new ArgumentNullException(nameof(usageRepository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<UsageReportDto> GetUsageReportAsync(UsageReportRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.OrganizationId == Guid.Empty)
        {
            throw new ArgumentException("Organization ID is required", nameof(request));
        }

        var cacheKey = $"usage_report_{request.OrganizationId}_{request.FromDate:yyyyMMdd}_{request.ToDate:yyyyMMdd}_{request.ResourceType}_{request.GroupBy}";

        if (_cache.TryGetValue(cacheKey, out UsageReportDto? cachedReport))
        {
            _logger.LogDebug("Returning cached usage report for organization {OrganizationId}", request.OrganizationId);
            return cachedReport!;
        }

        var usageRecords = await _usageRepository.GetByOrganizationAsync(
            request.OrganizationId, request.FromDate, request.ToDate, cancellationToken);

        if (!string.IsNullOrEmpty(request.ResourceType))
        {
            usageRecords = usageRecords.Where(u => u.ResourceType == request.ResourceType).ToList();
        }

        var groupedData = request.GroupBy?.ToLowerInvariant() switch
        {
            "week" => GroupByWeek(usageRecords),
            "month" => GroupByMonth(usageRecords),
            _ => GroupByDay(usageRecords)
        };

        var totalUsage = groupedData.Sum(g => g.Quantity);
        var totalCost = CalculateTotalCost(usageRecords);

        var items = groupedData
            .Select(g => new UsageReportItemDto(
                g.Period,
                g.ResourceType,
                g.Quantity,
                g.Unit,
                g.Cost,
                totalUsage > 0 ? (double)(g.Quantity / totalUsage * 100) : 0))
            .ToList();

        var report = new UsageReportDto(
            request.OrganizationId,
            request.FromDate,
            request.ToDate,
            totalUsage,
            totalCost,
            items);

        _cache.Set(cacheKey, report, CacheDuration);
        return report;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UsageByResourceDto>> GetUsageByResourceAsync(
        Guid organizationId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("Organization ID is required", nameof(organizationId));
        }

        var usageRecords = await _usageRepository.GetByOrganizationAsync(organizationId, from, to, cancellationToken);

        var groupedByResource = usageRecords
            .GroupBy(u => u.ResourceType)
            .Select(g => new
            {
                ResourceType = g.Key,
                TotalQuantity = g.Sum(u => u.Quantity),
                Unit = g.First().Unit,
                TotalCost = CalculateCostForResource(g.ToList())
            })
            .ToList();

        var totalCost = groupedByResource.Sum(g => g.TotalCost);

        return groupedByResource
            .Select(g => new UsageByResourceDto(
                g.ResourceType,
                g.TotalQuantity,
                g.Unit,
                g.TotalCost,
                totalCost > 0 ? (double)(g.TotalCost / totalCost * 100) : 0))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UsageByTimeDto>> GetUsageOverTimeAsync(
        Guid organizationId, string resourceType, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("Organization ID is required", nameof(organizationId));
        }

        ArgumentException.ThrowIfNullOrEmpty(resourceType);

        var usageRecords = await _usageRepository.GetByOrganizationAsync(organizationId, from, to, cancellationToken);

        var filteredRecords = usageRecords.Where(u => u.ResourceType == resourceType).ToList();

        return filteredRecords
            .GroupBy(u => u.Timestamp.Date)
            .Select(g => new UsageByTimeDto(
                g.Key,
                g.Sum(u => u.Quantity),
                CalculateCostForResource(g.ToList())))
            .OrderBy(d => d.Period)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<UsageSummaryDto> GetUsageSummaryAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("Organization ID is required", nameof(organizationId));
        }

        var now = DateTime.UtcNow;
        var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var previousMonthStart = currentMonthStart.AddMonths(-1);
        var previousMonthEnd = currentMonthStart.AddDays(-1);

        var currentMonthUsage = await _usageRepository.GetByOrganizationAsync(
            organizationId, currentMonthStart, now, cancellationToken);
        var previousMonthUsage = await _usageRepository.GetByOrganizationAsync(
            organizationId, previousMonthStart, previousMonthEnd, cancellationToken);

        var currentMonthTotal = CalculateTotalCost(currentMonthUsage);
        var previousMonthTotal = CalculateTotalCost(previousMonthUsage);

        var monthOverMonthChange = previousMonthTotal > 0
            ? ((currentMonthTotal - previousMonthTotal) / previousMonthTotal) * 100
            : 0;

        var topResources = currentMonthUsage
            .GroupBy(u => u.ResourceType)
            .Select(g => new
            {
                ResourceType = g.Key,
                Usage = g.Sum(u => u.Quantity),
                Cost = CalculateCostForResource(g.ToList())
            })
            .OrderByDescending(r => r.Cost)
            .Take(5)
            .Select(r => new TopResourceDto(
                r.ResourceType,
                r.Usage,
                r.Cost,
                currentMonthTotal > 0 ? (double)(r.Cost / currentMonthTotal * 100) : 0))
            .ToList();

        return new UsageSummaryDto(
            organizationId,
            currentMonthUsage.Sum(u => u.Quantity),
            currentMonthTotal,
            previousMonthUsage.Sum(u => u.Quantity),
            previousMonthTotal,
            monthOverMonthChange,
            topResources);
    }

    /// <inheritdoc />
    public async Task<byte[]> ExportUsageReportAsync(UsageExportRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var usageRecords = await _usageRepository.GetByOrganizationAsync(
            request.OrganizationId, request.FromDate, request.ToDate, cancellationToken);

        return request.Format.ToLowerInvariant() switch
        {
            "json" => ExportAsJson(usageRecords),
            "csv" => ExportAsCsv(usageRecords),
            _ => ExportAsCsv(usageRecords)
        };
    }

    private static List<GroupedUsage> GroupByDay(IEnumerable<UsageRecord> records)
    {
        return records
            .GroupBy(r => new { r.Timestamp.Date, r.ResourceType })
            .Select(g => new GroupedUsage
            {
                Period = g.Key.Date,
                ResourceType = g.Key.ResourceType,
                Quantity = g.Sum(r => r.Quantity),
                Unit = g.First().Unit,
                Cost = CalculateCostForResource(g.ToList())
            })
            .ToList();
    }

    private static List<GroupedUsage> GroupByWeek(IEnumerable<UsageRecord> records)
    {
        return records
            .GroupBy(r => new { Week = GetWeekStart(r.Timestamp), r.ResourceType })
            .Select(g => new GroupedUsage
            {
                Period = g.Key.Week,
                ResourceType = g.Key.ResourceType,
                Quantity = g.Sum(r => r.Quantity),
                Unit = g.First().Unit,
                Cost = CalculateCostForResource(g.ToList())
            })
            .ToList();
    }

    private static List<GroupedUsage> GroupByMonth(IEnumerable<UsageRecord> records)
    {
        return records
            .GroupBy(r => new { Month = new DateTime(r.Timestamp.Year, r.Timestamp.Month, 1), r.ResourceType })
            .Select(g => new GroupedUsage
            {
                Period = g.Key.Month,
                ResourceType = g.Key.ResourceType,
                Quantity = g.Sum(r => r.Quantity),
                Unit = g.First().Unit,
                Cost = CalculateCostForResource(g.ToList())
            })
            .ToList();
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        var diff = date.DayOfWeek - DayOfWeek.Monday;
        if (diff < 0) diff += 7;
        return date.AddDays(-diff).Date;
    }

    private static decimal CalculateTotalCost(IEnumerable<UsageRecord> records)
    {
        return records.Sum(r => r.Quantity * GetUnitPrice(r.ResourceType));
    }

    private static decimal CalculateCostForResource(List<UsageRecord> records)
    {
        if (records.Count == 0) return 0;
        var unitPrice = GetUnitPrice(records[0].ResourceType);
        return records.Sum(r => r.Quantity * unitPrice);
    }

    private static decimal GetUnitPrice(string resourceType)
    {
        return resourceType switch
        {
            "API" => 0.01m,
            "Compute" => 0.50m,
            "Storage" => 0.02m,
            _ => 0.01m
        };
    }

    private static byte[] ExportAsJson(IEnumerable<UsageRecord> records)
    {
        var json = JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true });
        return Encoding.UTF8.GetBytes(json);
    }

    private static byte[] ExportAsCsv(IEnumerable<UsageRecord> records)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Timestamp,ResourceType,ResourceId,Quantity,Unit,OrganizationId");

        foreach (var record in records)
        {
            sb.AppendLine(
                $"{record.Timestamp:yyyy-MM-dd HH:mm:ss}," +
                $"{record.ResourceType}," +
                $"{record.ResourceId ?? ""}," +
                $"{record.Quantity.ToString(CultureInfo.InvariantCulture)}," +
                $"{record.Unit}," +
                $"{record.OrganizationId}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private class GroupedUsage
    {
        public DateTime Period { get; set; }
        public string ResourceType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal Cost { get; set; }
    }
}
