// <copyright file="BackgroundJobDto.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Orchestration.Application.DTOs;

using System;

/// <summary>
/// Represents a background job summary.
/// </summary>
/// <param name="Id">The job identifier.</param>
/// <param name="JobType">The job type.</param>
/// <param name="Status">The job status.</param>
/// <param name="Payload">The serialized payload.</param>
/// <param name="CreatedAt">The creation timestamp.</param>
/// <param name="ScheduledAt">The scheduled timestamp, if any.</param>
/// <param name="StartedAt">The start timestamp, if any.</param>
/// <param name="CompletedAt">The completion timestamp, if any.</param>
/// <param name="ErrorMessage">The error message, if any.</param>
/// <param name="RetryCount">The retry count.</param>
public record BackgroundJobDto(
    Guid Id,
    string JobType,
    string Status,
    string? Payload,
    DateTime CreatedAt,
    DateTime? ScheduledAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string? ErrorMessage,
    int RetryCount);
