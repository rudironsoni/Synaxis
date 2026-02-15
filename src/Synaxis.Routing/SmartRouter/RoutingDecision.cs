// <copyright file="RoutingDecision.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Routing.SmartRouter;

/// <summary>
/// Represents a routing decision made by the SmartRouter.
/// </summary>
public class RoutingDecision
{
    /// <summary>
    /// Gets or sets the selected provider for this request.
    /// </summary>
    public Provider SelectedProvider { get; set; } = new();

    /// <summary>
    /// Gets or sets the confidence score for this decision (0.0 to 1.0).
    /// </summary>
    public double ConfidenceScore { get; set; }

    /// <summary>
    /// Gets or sets the reasoning behind this routing decision.
    /// </summary>
    public string Reasoning { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason for this routing decision.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the alternative providers that could be used as fallbacks.
    /// </summary>
    public List<ProviderAlternative> AlternativeProviders { get; set; } = new();

    /// <summary>
    /// Gets or sets the predicted latency in milliseconds.
    /// </summary>
    public int PredictedLatencyMs { get; set; }

    /// <summary>
    /// Gets or sets the predicted cost for this request.
    /// </summary>
    public decimal PredictedCost { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this decision was made.
    /// </summary>
    public DateTime DecisionTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the timestamp when this decision was made.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the decision ID for tracking.
    /// </summary>
    public string DecisionId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the routing strategy used (e.g., "ml-prediction", "latency-based", "cost-based").
    /// </summary>
    public string RoutingStrategy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the feature values used for the prediction.
    /// </summary>
    public Dictionary<string, double> Features { get; set; } = new();
}
