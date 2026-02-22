// <copyright file="SmartRoutingEngine.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

#nullable enable

namespace Synaxis.Routing.SmartRouter
{
    using System.Collections.Concurrent;
    using Microsoft.Extensions.Logging;
    using Synaxis.Routing.CircuitBreaker;
    using Synaxis.Routing.Health;
    using CircuitBreakerImpl = Synaxis.Routing.CircuitBreaker.CircuitBreaker;

    /// <summary>
    /// SmartRoutingEngine with ML-based predictive routing for AI providers.
    /// </summary>
    public class SmartRoutingEngine : IRouter
    {
        private readonly SmartRouterOptions _options;
        private readonly RoutingPredictor _predictor;
        private readonly ProviderPerformanceTracker _performanceTracker;
        private readonly ConcurrentDictionary<string, Provider> _providers;
        private readonly ConcurrentDictionary<string, CircuitBreakerImpl> _circuitBreakers;
        private readonly RoutingMetrics _metrics;
        private readonly ILogger<SmartRoutingEngine>? _logger;
        private readonly Lock _lock = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="SmartRoutingEngine"/> class.
        /// </summary>
        /// <param name="options">The router options.</param>
        /// <param name="logger">The logger.</param>
        public SmartRoutingEngine(SmartRouterOptions? options = null, ILogger<SmartRoutingEngine>? logger = null)
        {
            this._options = options ?? new SmartRouterOptions();
            this._logger = logger;

            this._predictor = new RoutingPredictor(this._options.PredictorOptions);
            this._performanceTracker = new ProviderPerformanceTracker();
            this._providers = new ConcurrentDictionary<string, Provider>(StringComparer.OrdinalIgnoreCase);
            this._circuitBreakers = new ConcurrentDictionary<string, CircuitBreakerImpl>(StringComparer.OrdinalIgnoreCase);
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
            ArgumentNullException.ThrowIfNull(request);
            var availableProviders = this.GetAvailableProviders();
            if (availableProviders.Count == 0)
            {
                throw new InvalidOperationException("No available providers for routing.");
            }

            // Get predictions for all available providers
            var predictions = await this._predictor.PredictAsync(request, availableProviders, cancellationToken).ConfigureAwait(false);

            // Select the best provider
            var selectedPrediction = SelectBestProvider(predictions.ToList());

            // Create routing decision
            var decision = this.CreateRoutingDecision(selectedPrediction, predictions.ToList());

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
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task RecordRoutingResultAsync(
            RoutingDecision decision,
            bool success,
            double latencyMs,
            int inputTokens,
            int outputTokens,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(decision);
            this._performanceTracker.RecordRequest(decision.SelectedProvider.Id, success, (int)latencyMs, inputTokens, outputTokens, 0);

            // Update predictor with actual result for learning
            this._predictor.UpdatePrediction(
                decision.SelectedProvider.Id,
                decision.PredictedLatencyMs,
                latencyMs,
                decision.PredictedCost,
                0);

            this._logger?.LogInformation(
                "Recorded routing result for {DecisionId}: Success={Success}, Latency={LatencyMs}ms",
                decision.DecisionId,
                success,
                latencyMs);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Registers a provider with the router.
        /// </summary>
        /// <param name="provider">The provider to register.</param>
        public void RegisterProvider(Provider provider)
        {
            ArgumentNullException.ThrowIfNull(provider);
            this._providers[provider.Id] = provider;

            // Create circuit breaker for this provider
            var circuitBreakerOptions = this._options.CircuitBreakerOptions ?? new CircuitBreakerOptions();

            this._circuitBreakers[provider.Id] = new CircuitBreakerImpl(provider.Id, circuitBreakerOptions);

            this._logger?.LogInformation("Registered provider {ProviderId}", provider.Id);
        }

        /// <summary>
        /// Gets routing metrics for monitoring and analysis.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>Routing metrics.</returns>
        public Task<RoutingMetrics> GetRoutingMetricsAsync(CancellationToken cancellationToken = default)
        {
            lock (this._lock)
            {
                // Return a copy to avoid race conditions
                var metricsCopy = new RoutingMetrics
                {
                    TotalDecisions = this._metrics.TotalDecisions,
                    LastUpdated = this._metrics.LastUpdated,
                    ProviderSelectionCounts = this._metrics.ProviderSelectionCounts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase),
                };

                return Task.FromResult(metricsCopy);
            }
        }

        /// <summary>
        /// Gets available providers for a routing request.
        /// </summary>
        /// <returns>List of available providers.</returns>
        private List<Provider> GetAvailableProviders()
        {
            return this._providers.Values
                .Where(provider => HasRequiredCapabilities(provider, new List<string>()))
                .Where(provider => !this._circuitBreakers.TryGetValue(provider.Id, out var circuitBreaker) || circuitBreaker.AllowRequest())
                .Where(this.IsProviderHealthy)
                .ToList();
        }

        /// <summary>
        /// Checks if a provider has all required capabilities.
        /// </summary>
        /// <param name="provider">The provider to check.</param>
        /// <param name="requiredCapabilities">The required capabilities.</param>
        /// <returns>True if provider has all required capabilities.</returns>
        private static bool HasRequiredCapabilities(Provider provider, List<string> requiredCapabilities)
        {
            if (requiredCapabilities == null || requiredCapabilities.Count == 0)
            {
                return true;
            }

            return requiredCapabilities.All(capability => provider.Capabilities.Contains(capability, StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Checks if a provider is healthy based on recent performance.
        /// </summary>
        /// <param name="provider">The provider to check.</param>
        /// <returns>True if provider is healthy.</returns>
        private bool IsProviderHealthy(Provider provider)
        {
            var health = this._performanceTracker.GetHealthStatus(provider.Id);
            return health != ProviderHealthStatus.Unhealthy;
        }

        /// <summary>
        /// Selects the best provider based on predictions and heuristics.
        /// </summary>
        /// <param name="predictions">The predictions for all providers.</param>
        /// <returns>The best prediction.</returns>
        private static ProviderPrediction SelectBestProvider(List<ProviderPrediction> predictions)
        {
            if (predictions.Count == 0)
            {
                throw new InvalidOperationException("No predictions available.");
            }

            // Sort by score (higher is better)
            predictions.Sort((a, b) => b.Score.CompareTo(a.Score));

            // Return the best provider
            return predictions[0];
        }

        /// <summary>
        /// Creates a routing decision from a prediction.
        /// </summary>
        /// <param name="selectedPrediction">The selected prediction.</param>
        /// <param name="allPredictions">All predictions for alternatives.</param>
        /// <returns>A routing decision.</returns>
        private RoutingDecision CreateRoutingDecision(ProviderPrediction selectedPrediction, List<ProviderPrediction> allPredictions)
        {
            var alternatives = allPredictions
                .Where(p => !string.Equals(p.Provider.Id, selectedPrediction.Provider.Id, StringComparison.Ordinal))
                .Take(2)
                .Select(p => new ProviderAlternative
                {
                    ProviderId = p.Provider.Id,
                    ProviderName = p.Provider.Name,
                    ConfidenceScore = p.Confidence,
                })
                .ToList();

            return new RoutingDecision
            {
                DecisionId = Guid.NewGuid().ToString(),
                SelectedProvider = selectedPrediction.Provider,
                ConfidenceScore = selectedPrediction.Confidence,
                PredictedLatencyMs = (int)selectedPrediction.PredictedLatencyMs,
                AlternativeProviders = alternatives,
                DecisionTime = DateTime.UtcNow,
                Reasoning = $"Selected based on score {selectedPrediction.Score:F2}",
            };
        }
    }
}
