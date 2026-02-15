// <copyright file="HistoricalRoutingData.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Routing.SmartRouter;

/// <summary>
/// Represents historical routing data for training.
/// </summary>
public class HistoricalRoutingData
{
    /// <summary>
    /// Gets or sets the provider ID.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the predicted latency.
    /// </summary>
    public double PredictedLatencyMs { get; set; }

    /// <summary>
    /// Gets or sets the actual latency.
    /// </summary>
    public double ActualLatencyMs { get; set; }

    /// <summary>
    /// Gets or sets the predicted cost.
    /// </summary>
    public decimal PredictedCost { get; set; }

    /// <summary>
    /// Gets or sets the actual cost.
    /// </summary>
    public decimal ActualCost { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the routing decision.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
