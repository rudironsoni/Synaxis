// <copyright file="AuditReportRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Application.Services.Audit;

using System;

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
    string ReportType = "summary");
