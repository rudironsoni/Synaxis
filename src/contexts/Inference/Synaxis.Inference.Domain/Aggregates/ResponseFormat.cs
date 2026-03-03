// <copyright file="ResponseFormat.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Aggregates;

/// <summary>
/// Represents response format options.
/// </summary>
public enum ResponseFormat
{
    /// <summary>
    /// Plain text response.
    /// </summary>
    Text,

    /// <summary>
    /// JSON response.
    /// </summary>
    Json,

    /// <summary>
    /// Markdown response.
    /// </summary>
    Markdown,

    /// <summary>
    /// Code response.
    /// </summary>
    Code,
}
