// <copyright file="WebhookQueryRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Orchestration.Application.DTOs;

/// <summary>
/// Request to query webhooks.
/// </summary>
/// <param name="EventType">The event type filter.</param>
/// <param name="IsActive">The active status filter.</param>
/// <param name="Page">The page number.</param>
/// <param name="PageSize">The page size.</param>
public record WebhookQueryRequest(
    string? EventType = null,
    bool? IsActive = null,
    int Page = 1,
    int PageSize = 50);
