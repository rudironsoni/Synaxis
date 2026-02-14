using System.Threading.Tasks;

namespace Synaxis.Routing.SmartRouter;

/// <summary>
/// Represents an AI provider that can be selected for routing.
/// </summary>
public class Provider
{
    /// <summary>
    /// Gets or sets the unique identifier for the provider.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the provider (e.g., "OpenAI", "Anthropic", "Azure OpenAI").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model name (e.g., "gpt-4", "claude-3-opus").
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the base URL for the provider's API.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the priority of this provider (lower values = higher priority).
    /// </summary>
    public int Priority { get; set; } = 100;

    /// <summary>
    /// Gets or sets whether this provider is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the cost per 1K input tokens.
    /// </summary>
    public decimal CostPer1KInputTokens { get; set; }

    /// <summary>
    /// Gets or sets the cost per 1K output tokens.
    /// </summary>
    public decimal CostPer1KOutputTokens { get; set; }

    /// <summary>
    /// Gets or sets the maximum tokens supported by this provider.
    /// </summary>
    public int MaxTokens { get; set; } = 4096;

    /// <summary>
    /// Gets or sets the rate limit in requests per minute.
    /// </summary>
    public int RateLimitRpm { get; set; } = 60;
}

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

/// <summary>
/// Provides routing metrics for monitoring and analysis.
/// </summary>
public class RoutingMetrics
{
    /// <summary>
    /// Gets or sets the total number of routing decisions made.
    /// </summary>
    public int TotalDecisions { get; set; }

    /// <summary>
    /// Gets or sets the number of successful requests.
    /// </summary>
    public int SuccessfulRequests { get; set; }

    /// <summary>
    /// Gets or sets the number of failed requests.
    /// </summary>
    public int FailedRequests { get; set; }

    /// <summary>
    /// Gets or sets the number of fallback executions.
    /// </summary>
    public int FallbackExecutions { get; set; }

    /// <summary>
    /// Gets or sets the average latency in milliseconds.
    /// </summary>
    public double AverageLatencyMs { get; set; }

    /// <summary>
    /// Gets or sets the total cost incurred.
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Gets or sets the provider selection counts.
    /// </summary>
    public Dictionary<string, int> ProviderSelectionCounts { get; set; } = new();

    /// <summary>
    /// Gets or sets the timestamp of the last update.
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Interface for routing requests to AI providers.
/// </summary>
public interface IRouter
{
    /// <summary>
    /// Routes a request to the optimal provider based on ML predictions and heuristics.
    /// </summary>
    /// <param name="request">The routing request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A routing decision containing the selected provider and metadata.</returns>
    Task<RoutingDecision> RouteRequestAsync(RoutingRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records the result of a routing decision for learning and metrics.
    /// </summary>
    /// <param name="decision">The routing decision that was made.</param>
    /// <param name="success">Whether the request was successful.</param>
    /// <param name="latencyMs">The actual latency in milliseconds.</param>
    /// <param name="inputTokens">The actual number of input tokens used.</param>
    /// <param name="outputTokens">The actual number of output tokens used.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    Task RecordRoutingResultAsync(
        RoutingDecision decision,
        bool success,
        int latencyMs,
        int inputTokens,
        int outputTokens,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current routing metrics.
    /// </summary>
    /// <returns>The routing metrics.</returns>
    Task<RoutingMetrics> GetRoutingMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the performance metrics for a specific provider.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The provider performance metrics.</returns>
    Task<ProviderPerformanceMetrics?> GetProviderMetricsAsync(string providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all registered providers.
    /// </summary>
    /// <returns>A list of all registered providers.</returns>
    Task<IReadOnlyList<Provider>> GetProvidersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds or updates a provider.
    /// </summary>
    /// <param name="provider">The provider to add or update.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    Task AddOrUpdateProviderAsync(Provider provider, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a provider.
    /// </summary>
    /// <param name="providerId">The provider ID to remove.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    Task RemoveProviderAsync(string providerId, CancellationToken cancellationToken = default);
}
