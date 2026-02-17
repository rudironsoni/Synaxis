// <copyright file="AlternativeOption.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Aggregates;

/// <summary>
/// Represents an alternative routing option.
/// </summary>
public class AlternativeOption
{
    /// <summary>
    /// Gets or sets the provider.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the score.
    /// </summary>
    public double Score { get; set; }
}
