// <copyright file="RoutingDecision.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Domain.ValueObjects;

/// <summary>
/// Represents a routing decision for an inference request.
/// </summary>
public class RoutingDecision
{
    /// <summary>
    /// Gets or sets the selected provider identifier.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the selected model identifier.
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
    /// Gets or sets the estimated latency in milliseconds.
    /// </summary>
    public double EstimatedLatencyMs { get; set; }

    /// <summary>
    /// Gets or sets the estimated cost.
    /// </summary>
    public decimal EstimatedCost { get; set; }
}
