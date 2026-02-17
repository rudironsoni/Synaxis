// <copyright file="RoutingDecision.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.Aggregates;

/// <summary>
/// Represents a routing decision.
/// </summary>
public class RoutingDecision
{
    /// <summary>
    /// Gets or sets the selected provider.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the selected model.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the routing reason.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the routing score.
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Gets or sets alternative options.
    /// </summary>
    public IList<AlternativeOption> Alternatives { get; set; } = new List<AlternativeOption>();
}
