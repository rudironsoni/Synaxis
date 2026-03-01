// <copyright file="AuditReportRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services.Audit;

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
