// <copyright file="RoutingPredictorOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Routing.SmartRouter;

/// <summary>
/// Configuration options for the routing predictor.
/// </summary>
public class RoutingPredictorOptions
{
    /// <summary>
    /// Gets or sets the weight for latency in the scoring algorithm (0.0 to 1.0).
    /// Default is 0.4.
    /// </summary>
    public double LatencyWeight { get; set; } = 0.4;

    /// <summary>
    /// Gets or sets the weight for cost in the scoring algorithm (0.0 to 1.0).
    /// Default is 0.3.
    /// </summary>
    public double CostWeight { get; set; } = 0.3;

    /// <summary>
    /// Gets or sets the weight for success rate in the scoring algorithm (0.0 to 1.0).
    /// Default is 0.2.
    /// </summary>
    public double SuccessRateWeight { get; set; } = 0.2;

    /// <summary>
    /// Gets or sets the weight for priority in the scoring algorithm (0.0 to 1.0).
    /// Default is 0.1.
    /// </summary>
    public double PriorityWeight { get; set; } = 0.1;

    /// <summary>
    /// Gets or sets the minimum number of requests required before using ML predictions.
    /// Default is 10.
    /// </summary>
    public int MinRequestsForPrediction { get; set; } = 10;

    /// <summary>
    /// Gets or sets the confidence threshold for using ML predictions (0.0 to 1.0).
    /// Default is 0.6.
    /// </summary>
    public double ConfidenceThreshold { get; set; } = 0.6;

    /// <summary>
    /// Gets or sets a value indicating whether to use ML predictions or fall back to heuristics.
    /// Default is true.
    /// </summary>
    public bool UseMlPredictions { get; set; } = true;

    /// <summary>
    /// Gets or sets the learning rate for updating predictions.
    /// Default is 0.1.
    /// </summary>
    public double LearningRate { get; set; } = 0.1;
}
