// <copyright file="WebhookStatus.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Orchestration.Domain.Aggregates;

/// <summary>
/// Represents the status of a webhook.
/// </summary>
public enum WebhookStatus
{
    /// <summary>
    /// Webhook is active and receiving events.
    /// </summary>
    Active,

    /// <summary>
    /// Webhook is inactive and not receiving events.
    /// </summary>
    Inactive,

    /// <summary>
    /// Webhook has been deleted.
    /// </summary>
    Deleted,

    /// <summary>
    /// Webhook is failing repeatedly and may be disabled.
    /// </summary>
    Failing,
}
