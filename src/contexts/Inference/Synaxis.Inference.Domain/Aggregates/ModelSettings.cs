// <copyright file="ModelSettings.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Aggregates;

/// <summary>
/// Represents model settings.
/// </summary>
public class ModelSettings
{
    /// <summary>
    /// Gets or sets the maximum tokens.
    /// </summary>
    public int MaxTokens { get; set; } = 4096;

    /// <summary>
    /// Gets or sets the temperature.
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Gets or sets the top P.
    /// </summary>
    public double TopP { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the frequency penalty.
    /// </summary>
    public double FrequencyPenalty { get; set; } = 0.0;

    /// <summary>
    /// Gets or sets the presence penalty.
    /// </summary>
    public double PresencePenalty { get; set; } = 0.0;

    /// <summary>
    /// Gets or sets the context window size.
    /// </summary>
    public int ContextWindow { get; set; } = 8192;

    /// <summary>
    /// Gets or sets the stop sequences.
    /// </summary>
    public IList<string> StopSequences { get; set; } = new List<string>();
}
