namespace Synaxis.Routing.SmartRouter;

/// <summary>
/// Represents a routing request.
/// </summary>
public class RoutingRequest
{
    /// <summary>
    /// Gets or sets the tenant ID for the request.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user ID for the request.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the estimated number of input tokens.
    /// </summary>
    public int EstimatedInputTokens { get; set; }

    /// <summary>
    /// Gets or sets the estimated number of output tokens.
    /// </summary>
    public int EstimatedOutputTokens { get; set; }

    /// <summary>
    /// Gets or sets the priority of the request (lower values = higher priority).
    /// </summary>
    public int Priority { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum acceptable latency in milliseconds.
    /// </summary>
    public int MaxLatencyMs { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the maximum acceptable cost.
    /// </summary>
    public decimal MaxCost { get; set; } = decimal.MaxValue;

    /// <summary>
    /// Gets or sets the preferred provider ID (if any).
    /// </summary>
    public string? PreferredProviderId { get; set; }

    /// <summary>
    /// Gets or sets the excluded provider IDs.
    /// </summary>
    public string[] ExcludedProviderIds { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the request metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}
