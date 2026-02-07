// <copyright file="RequiredCapabilities.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Routing;

/// <summary>
/// Defines the capabilities required by an inference request for provider matching.
/// </summary>
public class RequiredCapabilities
{
    /// <summary>
    /// Gets a default instance with all capabilities set to false.
    /// </summary>
    public static RequiredCapabilities Default => new ();

    /// <summary>
    /// Gets or sets a value indicating whether streaming response support is required.
    /// </summary>
    public bool Streaming { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether function/tool calling support is required.
    /// </summary>
    public bool Tools { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether vision/image input support is required.
    /// </summary>
    public bool Vision { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether structured output (e.g., JSON schema) support is required.
    /// </summary>
    public bool StructuredOutput { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether log probabilities support is required.
    /// </summary>
    public bool LogProbs { get; set; }
}
