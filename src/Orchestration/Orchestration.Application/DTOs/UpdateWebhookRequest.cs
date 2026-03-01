// <copyright file="UpdateWebhookRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Orchestration.Application.DTOs;

/// <summary>
/// Request to update a webhook subscription.
/// </summary>
/// <param name="Url">The webhook endpoint URL.</param>
/// <param name="EventType">The event type.</param>
/// <param name="IsActive">Whether the webhook is active.</param>
/// <param name="Secret">The optional signing secret.</param>
public record UpdateWebhookRequest(
    string Url,
    string EventType,
    bool IsActive,
    string? Secret = null);
