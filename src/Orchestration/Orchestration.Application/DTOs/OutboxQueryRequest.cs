// <copyright file="OutboxQueryRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Orchestration.Application.DTOs;

/// <summary>
/// Request to query outbox messages.
/// </summary>
/// <param name="Status">The status filter.</param>
/// <param name="EventType">The event type filter.</param>
/// <param name="Page">The page number.</param>
/// <param name="PageSize">The page size.</param>
public record OutboxQueryRequest(
    string? Status = null,
    string? EventType = null,
    int Page = 1,
    int PageSize = 50);
