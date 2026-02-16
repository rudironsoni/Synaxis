// <copyright file="SmartRouter.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

#pragma warning disable MA0049 // Type name matches namespace - this is intentional for the SmartRouter pattern
namespace Synaxis.Routing.SmartRouter;

using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Logging;
using Synaxis.Routing.CircuitBreaker;
using CircuitBreakerImpl = Synaxis.Routing.CircuitBreaker.CircuitBreaker;

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
    private readonly Lock _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SmartRouter"/> class.
    /// </summary>
    /// <param name="options">The router options.</param>
    /// <param name="logger">The logger.</param>
    public SmartRouter(SmartRouterOptions? options = null, ILogger<SmartRouter>? logger = null)
    {
        this._options = options ?? new SmartRouterOptions();
        this._logger = logger;

        this._predictor = new RoutingPredictor(this._options.PredictorOptions);
        this._performanceTracker = new ProviderPerformanceTracker();
        this._providers = new ConcurrentDictionary<string, Provider>(StringComparer.Ordinal);
        this._circuitBreakers = new ConcurrentDictionary<string, CircuitBreakerImpl>(StringComparer.Ordinal);
        this._metrics = new RoutingMetrics();
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

        var availableProviders = this.GetAvailableProviders(request);
        if (availableProviders.Count == 0)
        {
            throw new InvalidOperationException("No available providers for routing.");
        }

        // Get predictions for all available providers
        var predictions = await this._predictor.PredictAsync(request, availableProviders, cancellationToken).ConfigureAwait(false);

        // Select the best provider
        var selectedPrediction = this.SelectBestProvider(predictions, request);

        // Create routing decision
        var decision = this.CreateRoutingDecision(selectedPrediction, predictions);

        // Update metrics
        lock (this._lock)
        {
            this._metrics.TotalDecisions++;
            if (!this._metrics.ProviderSelectionCounts.ContainsKey(decision.SelectedProvider.Id))
            {
                this._metrics.ProviderSelectionCounts[decision.SelectedProvider.Id] = 0;
            }

            this._metrics.ProviderSelectionCounts[decision.SelectedProvider.Id]++;
            this._metrics.LastUpdated = DateTime.UtcNow;
        }

        this._logger?.LogInformation(
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
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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
        this._performanceTracker.RecordRequest(providerId, success, latencyMs, inputTokens, outputTokens, cost);

        // Update circuit breaker
        if (this._options.EnableCircuitBreaker && this._circuitBreakers.TryGetValue(providerId, out var circuitBreaker))
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
        this._predictor.UpdatePrediction(
            providerId,
            decision.PredictedLatencyMs,
            latencyMs,
            decision.PredictedCost,
            cost);

        // Update routing metrics
        lock (this._lock)
        {
            if (success)
            {
                this._metrics.SuccessfulRequests++;
            }
            else
            {
                this._metrics.FailedRequests++;
            }

            this._metrics.TotalCost += cost;
            this._metrics.AverageLatencyMs = this.CalculateAverageLatency(latencyMs);
            this._metrics.LastUpdated = DateTime.UtcNow;
        }

        this._logger?.LogInformation(
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
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The routing metrics.</returns>
    public Task<RoutingMetrics> GetRoutingMetricsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(this._metrics);
    }

    /// <summary>
    /// Gets the performance metrics for a specific provider.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The provider performance metrics.</returns>
    public Task<ProviderPerformanceMetrics?> GetProviderMetricsAsync(string providerId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(this._performanceTracker.GetMetrics(providerId));
    }

    /// <summary>
    /// Gets all registered providers.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A list of all registered providers.</returns>
    public Task<IReadOnlyList<Provider>> GetProvidersAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Provider>>(this._providers.Values.ToList());
    }

    /// <summary>
    /// Adds or updates a provider.
    /// </summary>
    /// <param name="provider">The provider to add or update.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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

        this._providers.AddOrUpdate(provider.Id, provider, (_, _) => provider);

        // Create circuit breaker for the provider if enabled
        if (this._options.EnableCircuitBreaker)
        {
            var circuitBreakerOptions = this._options.CircuitBreakerOptions ?? new CircuitBreakerOptions();
            var circuitBreaker = new CircuitBreakerImpl($"provider-{provider.Id}", circuitBreakerOptions);
            this._circuitBreakers.AddOrUpdate(provider.Id, circuitBreaker, (_, _) => circuitBreaker);
        }

        this._logger?.LogInformation("Added/Updated provider {ProviderId} ({ProviderName})", provider.Id, provider.Name);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes a provider.
    /// </summary>
    /// <param name="providerId">The provider ID to remove.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task RemoveProviderAsync(string providerId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(providerId))
        {
            throw new ArgumentException("Provider ID cannot be null or empty.", nameof(providerId));
        }

        this._providers.TryRemove(providerId, out _);
        this._circuitBreakers.TryRemove(providerId, out _);
        this._performanceTracker.ResetMetrics(providerId);

        this._logger?.LogInformation("Removed provider {ProviderId}", providerId);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the circuit breaker for a provider.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    /// <returns>The circuit breaker, or null if not found.</returns>
    public CircuitBreakerImpl? GetCircuitBreaker(string providerId)
    {
        return this._circuitBreakers.TryGetValue(providerId, out var circuitBreaker) ? circuitBreaker : null;
    }

    /// <summary>
    /// Gets the performance tracker.
    /// </summary>
    /// <returns>The performance tracker.</returns>
    public ProviderPerformanceTracker GetPerformanceTracker()
    {
        return this._performanceTracker;
    }

    /// <summary>
    /// Gets the routing predictor.
    /// </summary>
    /// <returns>The routing predictor.</returns>
    public RoutingPredictor GetPredictor()
    {
        return this._predictor;
    }

    private List<Provider> GetAvailableProviders(RoutingRequest request)
    {
        var availableProviders = new List<Provider>();

        foreach (var provider in this._providers.Values)
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
            if (this._options.EnableCircuitBreaker &&
                this._circuitBreakers.TryGetValue(provider.Id, out var circuitBreaker) &&
                !circuitBreaker.AllowRequest())
            {
                this._logger?.LogWarning("Provider {ProviderId} circuit breaker is open, skipping", provider.Id);
                continue;
            }

            availableProviders.Add(provider);
        }

        return availableProviders;
    }

    private ProviderPrediction SelectBestProvider(IList<ProviderPrediction> predictions, RoutingRequest request)
    {
        // Check if preferred provider is available and meets requirements
        if (!string.IsNullOrEmpty(request.PreferredProviderId))
        {
            var preferred = predictions.FirstOrDefault(p => string.Equals(p.Provider.Id, request.PreferredProviderId, StringComparison.Ordinal));
            if (preferred != null && preferred.Confidence >= this._options.MinConfidenceThreshold)
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
        IList<ProviderPrediction> allPredictions)
    {
        var decision = new RoutingDecision
        {
            SelectedProvider = selectedPrediction.Provider,
            ConfidenceScore = selectedPrediction.Confidence,
            PredictedLatencyMs = (int)selectedPrediction.PredictedLatencyMs,
            PredictedCost = selectedPrediction.PredictedCost,
            RoutingStrategy = "ml-prediction",
            Features = selectedPrediction.Features,
        };

        // Build reasoning
        var reasoning = new List<string>
        {
            $"Selected provider: {selectedPrediction.Provider.Name} ({selectedPrediction.Provider.Model})",
            $"Predicted latency: {selectedPrediction.PredictedLatencyMs:F0}ms",
            $"Predicted cost: ${selectedPrediction.PredictedCost:F4}",
            $"Confidence: {selectedPrediction.Confidence:P2}",
        };

        // Add alternative providers
        var alternatives = allPredictions
            .Where(p => !string.Equals(p.Provider.Id, selectedPrediction.Provider.Id, StringComparison.Ordinal))
            .Take(this._options.MaxFallbackAttempts)
            .Select(p => new ProviderAlternative
            {
                Provider = p.Provider,
                Score = p.Score,
                Reason = $"Alternative with score {p.Score:F2}",
                PredictedLatencyMs = (int)p.PredictedLatencyMs,
                PredictedCost = p.PredictedCost,
            })
            .ToList();

        decision.AlternativeProviders = alternatives;
        decision.Reasoning = string.Join("; ", reasoning);

        return decision;
    }

    private static decimal CalculateActualCost(Provider provider, int inputTokens, int outputTokens)
    {
        var inputCost = (inputTokens / 1000.0m) * provider.CostPer1KInputTokens;
        var outputCost = (outputTokens / 1000.0m) * provider.CostPer1KOutputTokens;
        return inputCost + outputCost;
    }

    private double CalculateAverageLatency(int newLatencyMs)
    {
        var totalRequests = this._metrics.SuccessfulRequests + this._metrics.FailedRequests;
        if (totalRequests == 0)
        {
            return newLatencyMs;
        }

        var currentAverage = this._metrics.AverageLatencyMs;
        return ((currentAverage * (totalRequests - 1)) + newLatencyMs) / totalRequests;
    }
}
