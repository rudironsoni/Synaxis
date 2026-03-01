// <copyright file="CreateJobRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Orchestration.Application.DTOs;

using System;

/// <summary>
/// Request to create a background job.
/// </summary>
/// <param name="JobType">The job type.</param>
/// <param name="Payload">The serialized payload.</param>
/// <param name="ScheduledAt">The scheduled timestamp, if any.</param>
public record CreateJobRequest(
    string JobType,
    string Payload,
    DateTime? ScheduledAt = null);
