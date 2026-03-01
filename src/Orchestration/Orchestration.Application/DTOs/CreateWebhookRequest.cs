// <copyright file="CreateWebhookRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Orchestration.Application.DTOs;

/// <summary>
/// Request to create a webhook subscription.
/// </summary>
/// <param name="Url">The webhook endpoint URL.</param>
/// <param name="EventType">The event type.</param>
/// <param name="Secret">The optional signing secret.</param>
public record CreateWebhookRequest(
    string Url,
    string EventType,
    string? Secret = null);
