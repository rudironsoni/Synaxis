// <copyright file="AuditExportRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Application.Services.Audit;

using System;

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
    string Format = "json");
