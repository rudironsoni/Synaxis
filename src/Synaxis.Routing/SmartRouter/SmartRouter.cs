using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Synaxis.Routing.CircuitBreaker;
using CircuitBreakerImpl = Synaxis.Routing.CircuitBreaker.CircuitBreaker;

namespace Synaxis.Routing.SmartRouter;

/// <summary>
/// Configuration options for the SmartRouter.
/// </summary>
public class SmartRouterOptions
{
    /// <summary>
    /// Gets or sets the maximum number of fallback attempts.
    /// Default is 3.
    /// </summary>
    public int MaxFallbackAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the minimum confidence threshold for routing decisions.
    /// Default is 0.5.
    /// </summary>
    public double MinConfidenceThreshold { get; set; } = 0.5;

    /// <summary>
    /// Gets or sets whether to enable circuit breaker integration.
    /// Default is true.
    /// </summary>
    public bool EnableCircuitBreaker { get; set; } = true;

    /// <summary>
    /// Gets or sets the circuit breaker options.
    /// </summary>
    public CircuitBreakerOptions? CircuitBreakerOptions { get; set; }

    /// <summary>
    /// Gets or sets the routing predictor options.
    /// </summary>
    public RoutingPredictorOptions? PredictorOptions { get; set; }

    /// <summary>
    /// Gets or sets the default fallback provider ID.
    /// </summary>
    public string? DefaultFallbackProviderId { get; set; }
}

/// <summary>
/// SmartRouter with ML-based predictive routing for AI providers.
/// </summary>
public class SmartRouter : IRouter
{
    private readonly SmartRouterOptions _options;
    private readonly RoutingPredictor _predictor;
    private readonly ProviderPerformanceTracker _performanceTracker;
    private readonly ConcurrentDictionary<string, Provider> _providers;
    private readonly ConcurrentDictionary<string, CircuitBreakerImpl> _circuitBreakers;
    private readonly RoutingMetrics _metrics;
    private readonly ILogger<SmartRouter>? _logger;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SmartRouter"/> class.
    /// </summary>
    /// <param name="options">The router options.</param>
    /// <param name="logger">The logger.</param>
    public SmartRouter(SmartRouterOptions? options = null, ILogger<SmartRouter>? logger = null)
    {
        _options = options ?? new SmartRouterOptions();
        _logger = logger;

        _predictor = new RoutingPredictor(_options.PredictorOptions);
        _performanceTracker = new ProviderPerformanceTracker();
        _providers = new ConcurrentDictionary<string, Provider>();
        _circuitBreakers = new ConcurrentDictionary<string, CircuitBreakerImpl>();
        _metrics = new RoutingMetrics();
    }

    /// <summary>
    /// Routes a request to the optimal provider based on ML predictions and heuristics.
    /// </summary>
    /// <param name="request">The routing request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A routing decision containing the selected provider and metadata.</returns>
    public async Task<RoutingDecision> RouteRequestAsync(RoutingRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var availableProviders = GetAvailableProviders(request);
        if (availableProviders.Count == 0)
        {
            throw new InvalidOperationException("No available providers for routing.");
        }

        // Get predictions for all available providers
        var predictions = await _predictor.PredictAsync(request, availableProviders, cancellationToken);

        // Select the best provider
        var selectedPrediction = SelectBestProvider(predictions, request);

        // Create routing decision
        var decision = CreateRoutingDecision(selectedPrediction, predictions);

        // Update metrics
        lock (_lock)
        {
            _metrics.TotalDecisions++;
            if (!_metrics.ProviderSelectionCounts.ContainsKey(decision.SelectedProvider.Id))
            {
                _metrics.ProviderSelectionCounts[decision.SelectedProvider.Id] = 0;
            }
            _metrics.ProviderSelectionCounts[decision.SelectedProvider.Id]++;
            _metrics.LastUpdated = DateTime.UtcNow;
        }

        _logger?.LogInformation(
            "Routed request {DecisionId} to provider {ProviderId} with confidence {Confidence:F2}",
            decision.DecisionId,
            decision.SelectedProvider.Id,
            decision.ConfidenceScore);

        return decision;
    }

    /// <summary>
    /// Records the result of a routing decision for learning and metrics.
    /// </summary>
    /// <param name="decision">The routing decision that was made.</param>
    /// <param name="success">Whether the request was successful.</param>
    /// <param name="latencyMs">The actual latency in milliseconds.</param>
    /// <param name="inputTokens">The actual number of input tokens used.</param>
    /// <param name="outputTokens">The actual number of output tokens used.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    public Task RecordRoutingResultAsync(
        RoutingDecision decision,
        bool success,
        int latencyMs,
        int inputTokens,
        int outputTokens,
        CancellationToken cancellationToken = default)
    {
        if (decision == null)
        {
            throw new ArgumentNullException(nameof(decision));
        }

        var providerId = decision.SelectedProvider.Id;
        var cost = CalculateActualCost(decision.SelectedProvider, inputTokens, outputTokens);

        // Record performance metrics
        _performanceTracker.RecordRequest(providerId, success, latencyMs, inputTokens, outputTokens, cost);

        // Update circuit breaker
        if (_options.EnableCircuitBreaker && _circuitBreakers.TryGetValue(providerId, out var circuitBreaker))
        {
            if (success)
            {
                circuitBreaker.RecordSuccess();
            }
            else
            {
                circuitBreaker.RecordFailure();
            }
        }

        // Update predictor with actual results
        _predictor.UpdatePrediction(
            providerId,
            decision.PredictedLatencyMs,
            latencyMs,
            decision.PredictedCost,
            cost);

        // Update routing metrics
        lock (_lock)
        {
            if (success)
            {
                _metrics.SuccessfulRequests++;
            }
            else
            {
                _metrics.FailedRequests++;
            }

            _metrics.TotalCost += cost;
            _metrics.AverageLatencyMs = CalculateAverageLatency(latencyMs);
            _metrics.LastUpdated = DateTime.UtcNow;
        }

        _logger?.LogInformation(
            "Recorded result for decision {DecisionId}: Success={Success}, Latency={Latency}ms, Cost={Cost}",
            decision.DecisionId,
            success,
            latencyMs,
            cost);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the current routing metrics.
    /// </summary>
    /// <returns>The routing metrics.</returns>
    public Task<RoutingMetrics> GetRoutingMetricsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_metrics);
    }

    /// <summary>
    /// Gets the performance metrics for a specific provider.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The provider performance metrics.</returns>
    public Task<ProviderPerformanceMetrics?> GetProviderMetricsAsync(string providerId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_performanceTracker.GetMetrics(providerId));
    }

    /// <summary>
    /// Gets all registered providers.
    /// </summary>
    /// <returns>A list of all registered providers.</returns>
    public Task<IReadOnlyList<Provider>> GetProvidersAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Provider>>(_providers.Values.ToList());
    }

    /// <summary>
    /// Adds or updates a provider.
    /// </summary>
    /// <param name="provider">The provider to add or update.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    public Task AddOrUpdateProviderAsync(Provider provider, CancellationToken cancellationToken = default)
    {
        if (provider == null)
        {
            throw new ArgumentNullException(nameof(provider));
        }

        if (string.IsNullOrEmpty(provider.Id))
        {
            throw new ArgumentException("Provider ID cannot be null or empty.", nameof(provider));
        }

        _providers.AddOrUpdate(provider.Id, provider, (_, _) => provider);

        // Create circuit breaker for the provider if enabled
        if (_options.EnableCircuitBreaker)
        {
            var circuitBreakerOptions = _options.CircuitBreakerOptions ?? new CircuitBreakerOptions();
            var circuitBreaker = new CircuitBreakerImpl($"provider-{provider.Id}", circuitBreakerOptions);
            _circuitBreakers.AddOrUpdate(provider.Id, circuitBreaker, (_, _) => circuitBreaker);
        }

        _logger?.LogInformation("Added/Updated provider {ProviderId} ({ProviderName})", provider.Id, provider.Name);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes a provider.
    /// </summary>
    /// <param name="providerId">The provider ID to remove.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    public Task RemoveProviderAsync(string providerId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(providerId))
        {
            throw new ArgumentException("Provider ID cannot be null or empty.", nameof(providerId));
        }

        _providers.TryRemove(providerId, out _);
        _circuitBreakers.TryRemove(providerId, out _);
        _performanceTracker.ResetMetrics(providerId);

        _logger?.LogInformation("Removed provider {ProviderId}", providerId);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the circuit breaker for a provider.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    /// <returns>The circuit breaker, or null if not found.</returns>
    public CircuitBreakerImpl? GetCircuitBreaker(string providerId)
    {
        return _circuitBreakers.TryGetValue(providerId, out var circuitBreaker) ? circuitBreaker : null;
    }

    /// <summary>
    /// Gets the performance tracker.
    /// </summary>
    /// <returns>The performance tracker.</returns>
    public ProviderPerformanceTracker GetPerformanceTracker()
    {
        return _performanceTracker;
    }

    /// <summary>
    /// Gets the routing predictor.
    /// </summary>
    /// <returns>The routing predictor.</returns>
    public RoutingPredictor GetPredictor()
    {
        return _predictor;
    }

    private List<Provider> GetAvailableProviders(RoutingRequest request)
    {
        var availableProviders = new List<Provider>();

        foreach (var provider in _providers.Values)
        {
            if (!provider.IsEnabled)
            {
                continue;
            }

            if (request.ExcludedProviderIds.Contains(provider.Id))
            {
                continue;
            }

            // Check circuit breaker state
            if (_options.EnableCircuitBreaker &&
                _circuitBreakers.TryGetValue(provider.Id, out var circuitBreaker) &&
                !circuitBreaker.AllowRequest())
            {
                _logger?.LogWarning("Provider {ProviderId} circuit breaker is open, skipping", provider.Id);
                continue;
            }

            availableProviders.Add(provider);
        }

        return availableProviders;
    }

    private ProviderPrediction SelectBestProvider(List<ProviderPrediction> predictions, RoutingRequest request)
    {
        // Check if preferred provider is available and meets requirements
        if (!string.IsNullOrEmpty(request.PreferredProviderId))
        {
            var preferred = predictions.FirstOrDefault(p => string.Equals(p.Provider.Id, request.PreferredProviderId, StringComparison.Ordinal));
            if (preferred != null && preferred.Confidence >= _options.MinConfidenceThreshold)
            {
                return preferred;
            }
        }

        // Select the best prediction based on score
        var bestPrediction = predictions.FirstOrDefault();
        if (bestPrediction == null)
        {
            throw new InvalidOperationException("No valid provider predictions available.");
        }

        return bestPrediction;
    }

    private RoutingDecision CreateRoutingDecision(
        ProviderPrediction selectedPrediction,
        List<ProviderPrediction> allPredictions)
    {
        var decision = new RoutingDecision
        {
            SelectedProvider = selectedPrediction.Provider,
            ConfidenceScore = selectedPrediction.Confidence,
            PredictedLatencyMs = (int)selectedPrediction.PredictedLatencyMs,
            PredictedCost = selectedPrediction.PredictedCost,
            RoutingStrategy = "ml-prediction",
            Features = selectedPrediction.Features
        };

        // Build reasoning
        var reasoning = new List<string>
        {
            $"Selected provider: {selectedPrediction.Provider.Name} ({selectedPrediction.Provider.Model})",
            $"Predicted latency: {selectedPrediction.PredictedLatencyMs:F0}ms",
            $"Predicted cost: ${selectedPrediction.PredictedCost:F4}",
            $"Confidence: {selectedPrediction.Confidence:P2}"
        };

        // Add alternative providers
        var alternatives = allPredictions
            .Where(p => !string.Equals(p.Provider.Id, selectedPrediction.Provider.Id, StringComparison.Ordinal))
            .Take(_options.MaxFallbackAttempts)
            .Select(p => new ProviderAlternative
            {
                Provider = p.Provider,
                Score = p.Score,
                Reason = $"Alternative with score {p.Score:F2}",
                PredictedLatencyMs = (int)p.PredictedLatencyMs,
                PredictedCost = p.PredictedCost
            })
            .ToList();

        decision.AlternativeProviders = alternatives;
        decision.Reasoning = string.Join("; ", reasoning);

        return decision;
    }

    private decimal CalculateActualCost(Provider provider, int inputTokens, int outputTokens)
    {
        var inputCost = (inputTokens / 1000.0m) * provider.CostPer1KInputTokens;
        var outputCost = (outputTokens / 1000.0m) * provider.CostPer1KOutputTokens;
        return inputCost + outputCost;
    }

    private double CalculateAverageLatency(int newLatencyMs)
    {
        var totalRequests = _metrics.SuccessfulRequests + _metrics.FailedRequests;
        if (totalRequests == 0)
        {
            return newLatencyMs;
        }

        var currentAverage = _metrics.AverageLatencyMs;
        return ((currentAverage * (totalRequests - 1)) + newLatencyMs) / totalRequests;
    }
}
