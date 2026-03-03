// <copyright file="DefaultSettingsUpdated.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Events;

using Synaxis.Abstractions.Cloud;
using Synaxis.Inference.Domain.Aggregates;

/// <summary>
/// Event raised when default settings are updated.
/// </summary>
public class DefaultSettingsUpdated : DomainEvent
{
    /// <summary>
    /// Gets or sets the preferences identifier.
    /// </summary>
    public Guid PreferencesId { get; set; }

    /// <summary>
    /// Gets or sets the system prompt.
    /// </summary>
    public string? SystemPrompt { get; set; }

    /// <summary>
    /// Gets or sets the temperature.
    /// </summary>
    public double Temperature { get; set; }

    /// <summary>
    /// Gets or sets the maximum tokens.
    /// </summary>
    public int MaxTokens { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable streaming.
    /// </summary>
    public bool EnableStreaming { get; set; }

    /// <summary>
    /// Gets or sets the response format.
    /// </summary>
    public ResponseFormat ResponseFormat { get; set; }

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <inheritdoc/>
    public override string AggregateId => this.PreferencesId.ToString();

    /// <inheritdoc/>
    public override string EventType => nameof(DefaultSettingsUpdated);
}
