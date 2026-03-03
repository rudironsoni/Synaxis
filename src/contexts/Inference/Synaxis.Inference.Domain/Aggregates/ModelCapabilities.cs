// <copyright file="ModelCapabilities.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Aggregates;

/// <summary>
/// Represents model capabilities.
/// </summary>
public class ModelCapabilities
{
    /// <summary>
    /// Gets or sets a value indicating whether the model supports streaming.
    /// </summary>
    public bool SupportsStreaming { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the model supports function calling.
    /// </summary>
    public bool SupportsFunctionCalling { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the model supports vision.
    /// </summary>
    public bool SupportsVision { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the model supports JSON mode.
    /// </summary>
    public bool SupportsJsonMode { get; set; } = false;

    /// <summary>
    /// Gets or sets the supported languages.
    /// </summary>
    public IList<string> SupportedLanguages { get; set; } = new List<string> { "en" };
}
