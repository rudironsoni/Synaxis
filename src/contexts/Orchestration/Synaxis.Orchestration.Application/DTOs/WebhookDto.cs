// <copyright file="WebhookDto.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Orchestration.Application.DTOs;

using System;

/// <summary>
/// Represents a webhook subscription.
/// </summary>
/// <param name="Id">The webhook identifier.</param>
/// <param name="Url">The webhook URL.</param>
/// <param name="EventType">The event type.</param>
/// <param name="IsActive">Whether the webhook is active.</param>
/// <param name="RetryCount">The retry count.</param>
/// <param name="CreatedAt">The creation timestamp.</param>
/// <param name="LastDeliveredAt">The last delivery timestamp.</param>
public record WebhookDto(
    Guid Id,
    string Url,
    string EventType,
    bool IsActive,
    int RetryCount,
    DateTime CreatedAt,
    DateTime? LastDeliveredAt);
