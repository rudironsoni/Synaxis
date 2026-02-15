namespace Synaxis.Routing.SmartRouter;

/// <summary>
/// Represents a prediction for a provider.
/// </summary>
public class ProviderPrediction
{
    /// <summary>
    /// Gets or sets the provider.
    /// </summary>
    public Provider Provider { get; set; } = new();

    /// <summary>
    /// Gets or sets the predicted latency in milliseconds.
    /// </summary>
    public double PredictedLatencyMs { get; set; }

    /// <summary>
    /// Gets or sets the predicted cost.
    /// </summary>
    public decimal PredictedCost { get; set; }

    /// <summary>
    /// Gets or sets the predicted success rate (0.0 to 1.0).
    /// </summary>
    public double PredictedSuccessRate { get; set; }

    /// <summary>
    /// Gets or sets the overall score (lower is better).
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Gets or sets the confidence in this prediction (0.0 to 1.0).
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Gets or sets the features used for this prediction.
    /// </summary>
    public Dictionary<string, double> Features { get; set; } = new();
}
