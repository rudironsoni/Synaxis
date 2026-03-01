// <copyright file="JobQueryRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Orchestration.Application.DTOs;

using System;

/// <summary>
/// Request to query background jobs.
/// </summary>
/// <param name="Status">The job status filter.</param>
/// <param name="JobType">The job type filter.</param>
/// <param name="From">The start date filter.</param>
/// <param name="To">The end date filter.</param>
/// <param name="Page">The page number.</param>
/// <param name="PageSize">The page size.</param>
public record JobQueryRequest(
    string? Status = null,
    string? JobType = null,
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 50);
