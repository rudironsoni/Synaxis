// <copyright file="IUsageReportingService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Billing.Application.Services;

/// <summary>
/// Service for querying and aggregating usage data for billing and analytics.
/// </summary>
public interface IUsageReportingService
{
    /// <summary>
    /// Gets a detailed usage report for an organization within a date range.
    /// </summary>
    Task<UsageReportDto> GetUsageReportAsync(UsageReportRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets usage aggregated by resource type.
    /// </summary>
    Task<IReadOnlyList<UsageByResourceDto>> GetUsageByResourceAsync(Guid organizationId, DateTime from, DateTime to, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets usage over time for a specific resource type.
    /// </summary>
    Task<IReadOnlyList<UsageByTimeDto>> GetUsageOverTimeAsync(Guid organizationId, string resourceType, DateTime from, DateTime to, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a summary of current and previous month usage.
    /// </summary>
    Task<UsageSummaryDto> GetUsageSummaryAsync(Guid organizationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports usage data in the specified format.
    /// </summary>
    Task<byte[]> ExportUsageReportAsync(UsageExportRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Request parameters for generating a usage report.
/// </summary>
public record UsageReportRequest(
    Guid OrganizationId,
    DateTime FromDate,
    DateTime ToDate,
    string? ResourceType = null,
    string? GroupBy = "day");

/// <summary>
/// Represents a complete usage report.
/// </summary>
public record UsageReportDto(
    Guid OrganizationId,
    DateTime FromDate,
    DateTime ToDate,
    decimal TotalUsage,
    decimal TotalCost,
    IReadOnlyList<UsageReportItemDto> Items);

/// <summary>
/// Represents a single item in a usage report.
/// </summary>
public record UsageReportItemDto(
    DateTime Period,
    string ResourceType,
    decimal Quantity,
    string Unit,
    decimal Cost,
    double PercentageOfTotal);

/// <summary>
/// Represents usage aggregated by resource type.
/// </summary>
public record UsageByResourceDto(
    string ResourceType,
    decimal TotalQuantity,
    string Unit,
    decimal TotalCost,
    double PercentageOfTotal);

/// <summary>
/// Represents usage data over time.
/// </summary>
public record UsageByTimeDto(
    DateTime Period,
    decimal Quantity,
    decimal Cost);

/// <summary>
/// Represents a summary of usage statistics.
/// </summary>
public record UsageSummaryDto(
    Guid OrganizationId,
    decimal CurrentMonthUsage,
    decimal CurrentMonthCost,
    decimal PreviousMonthUsage,
    decimal PreviousMonthCost,
    decimal MonthOverMonthChangePercent,
    IReadOnlyList<TopResourceDto> TopResources);

/// <summary>
/// Represents a top resource by usage.
/// </summary>
public record TopResourceDto(
    string ResourceType,
    decimal Usage,
    decimal Cost,
    double PercentageOfTotal);

/// <summary>
/// Request parameters for exporting usage data.
/// </summary>
public record UsageExportRequest(
    Guid OrganizationId,
    DateTime FromDate,
    DateTime ToDate,
    string Format = "csv");
