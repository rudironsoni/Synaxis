// <copyright file="AuditStatisticsDto.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Application.Services.Audit;

using System;
using System.Collections.Generic;

/// <summary>
/// Statistics for audit logs.
/// </summary>
/// <param name="TotalEvents">The total number of events.</param>
/// <param name="EventsByType">Events grouped by type.</param>
/// <param name="EventsByCategory">Events grouped by category.</param>
/// <param name="EventsOverTime">Events grouped by date.</param>
public record AuditStatisticsDto(
    int TotalEvents,
    IDictionary<string, int> EventsByType,
    IDictionary<string, int> EventsByCategory,
    IDictionary<DateTime, int> EventsOverTime);
