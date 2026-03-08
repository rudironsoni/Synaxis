// <copyright file="WorkflowSchedule.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Contracts.V2.DomainEvents;

/// <summary>
/// Workflow schedule definition.
/// </summary>
public record WorkflowSchedule
{
    /// <summary>
    /// Gets the cron expression for scheduling.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("cron")]
    public string? Cron { get; init; }

    /// <summary>
    /// Gets the time zone for the schedule.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("timezone")]
    public string Timezone { get; init; } = "UTC";

    /// <summary>
    /// Gets a value indicating whether the schedule is enabled.
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("enabled")]
    public bool Enabled { get; init; } = true;
}
