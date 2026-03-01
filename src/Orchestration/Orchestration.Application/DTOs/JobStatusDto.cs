// <copyright file="JobStatusDto.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Orchestration.Application.DTOs;

using System;

/// <summary>
/// Represents job progress status.
/// </summary>
/// <param name="Id">The job identifier.</param>
/// <param name="Status">The job status.</param>
/// <param name="ProgressPercent">The progress percentage.</param>
/// <param name="CurrentStep">The current step.</param>
/// <param name="EstimatedCompletion">The estimated completion timestamp.</param>
public record JobStatusDto(
    Guid Id,
    string Status,
    double ProgressPercent,
    string? CurrentStep,
    DateTime? EstimatedCompletion);
