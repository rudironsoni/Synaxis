namespace Synaxis.Routing.SmartRouter;

/// <summary>
/// Represents an alternative provider option for fallback.
/// </summary>
public class ProviderAlternative
{
    /// <summary>
    /// Gets or sets the alternative provider.
    /// </summary>
    public Provider Provider { get; set; } = new();

    /// <summary>
    /// Gets or sets the score for this alternative (lower is better).
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Gets or sets the reason this provider is an alternative.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the predicted latency for this alternative.
    /// </summary>
    public int PredictedLatencyMs { get; set; }

    /// <summary>
    /// Gets or sets the predicted cost for this alternative.
    /// </summary>
    public decimal PredictedCost { get; set; }
}
