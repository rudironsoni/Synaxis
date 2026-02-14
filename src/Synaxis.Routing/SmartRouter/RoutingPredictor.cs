using System.Collections.Concurrent;

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
    /// Gets or sets whether to use ML predictions or fall back to heuristics.
    /// Default is true.
    /// </summary>
    public bool UseMlPredictions { get; set; } = true;

    /// <summary>
    /// Gets or sets the learning rate for updating predictions.
    /// Default is 0.1.
    /// </summary>
    public double LearningRate { get; set; } = 0.1;
}

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

/// <summary>
/// Provides ML-based predictions for routing decisions.
/// </summary>
public class RoutingPredictor
{
    private readonly RoutingPredictorOptions _options;
    private readonly ProviderPerformanceTracker _performanceTracker;
    private readonly ConcurrentDictionary<string, double> _providerWeights;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RoutingPredictor"/> class.
    /// </summary>
    /// <param name="options">The predictor options.</param>
    /// <param name="performanceTracker">The performance tracker.</param>
    public RoutingPredictor(
        RoutingPredictorOptions? options = null,
        ProviderPerformanceTracker? performanceTracker = null)
    {
        _options = options ?? new RoutingPredictorOptions();
        _performanceTracker = performanceTracker ?? new ProviderPerformanceTracker();
        _providerWeights = new ConcurrentDictionary<string, double>();
    }

    /// <summary>
    /// Predicts the best provider for a routing request.
    /// </summary>
    /// <param name="request">The routing request.</param>
    /// <param name="providers">The available providers.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A list of provider predictions sorted by score (best first).</returns>
    public Task<List<ProviderPrediction>> PredictAsync(
        RoutingRequest request,
        IReadOnlyList<Provider> providers,
        CancellationToken cancellationToken = default)
    {
        var predictions = new List<ProviderPrediction>();

        foreach (var provider in providers)
        {
            if (!provider.IsEnabled)
            {
                continue;
            }

            if (request.ExcludedProviderIds.Contains(provider.Id))
            {
                continue;
            }

            var prediction = PredictForProvider(request, provider);
            predictions.Add(prediction);
        }

        // Sort by score (lower is better)
        predictions.Sort((a, b) => a.Score.CompareTo(b.Score));

        return Task.FromResult(predictions);
    }

    /// <summary>
    /// Predicts the performance for a specific provider.
    /// </summary>
    /// <param name="request">The routing request.</param>
    /// <param name="provider">The provider.</param>
    /// <returns>The provider prediction.</returns>
    public ProviderPrediction PredictForProvider(RoutingRequest request, Provider provider)
    {
        var metrics = _performanceTracker.GetMetrics(provider.Id);
        var features = ExtractFeatures(request, provider, metrics);
        var prediction = new ProviderPrediction
        {
            Provider = provider,
            Features = features
        };

        // Predict latency
        prediction.PredictedLatencyMs = PredictLatency(features, metrics);

        // Predict cost
        prediction.PredictedCost = PredictCost(request, provider);

        // Predict success rate
        prediction.PredictedSuccessRate = PredictSuccessRate(features, metrics);

        // Calculate confidence
        prediction.Confidence = CalculateConfidence(metrics);

        // Calculate overall score
        prediction.Score = CalculateScore(prediction, request);

        return prediction;
    }

    /// <summary>
    /// Updates the predictor with actual routing results.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    /// <param name="predictedLatencyMs">The predicted latency.</param>
    /// <param name="actualLatencyMs">The actual latency.</param>
    /// <param name="predictedCost">The predicted cost.</param>
    /// <param name="actualCost">The actual cost.</param>
    public void UpdatePrediction(
        string providerId,
        double predictedLatencyMs,
        double actualLatencyMs,
        decimal predictedCost,
        decimal actualCost)
    {
        // Update provider weights based on prediction accuracy
        var latencyError = Math.Abs(predictedLatencyMs - actualLatencyMs) / Math.Max(1, actualLatencyMs);
        var costError = Math.Abs((double)(predictedCost - actualCost)) / Math.Max(0.01, (double)actualCost);
        var totalError = (latencyError + costError) / 2.0;

        // Adjust weight based on error (lower error = higher weight)
        var adjustment = 1.0 - totalError;
        var currentWeight = _providerWeights.GetOrAdd(providerId, _ => 1.0);
        var newWeight = currentWeight + _options.LearningRate * (adjustment - currentWeight);
        _providerWeights.AddOrUpdate(providerId, newWeight, (_, _) => newWeight);
    }

    /// <summary>
    /// Trains the predictor with historical data.
    /// </summary>
    /// <param name="historicalData">The historical routing data.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the training operation.</returns>
    public Task TrainAsync(List<HistoricalRoutingData> historicalData, CancellationToken cancellationToken = default)
    {
        // Simple online learning: update weights based on historical accuracy
        foreach (var data in historicalData)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            UpdatePrediction(
                data.ProviderId,
                data.PredictedLatencyMs,
                data.ActualLatencyMs,
                data.PredictedCost,
                data.ActualCost);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the current provider weights.
    /// </summary>
    /// <returns>A dictionary of provider IDs to their weights.</returns>
    public Dictionary<string, double> GetProviderWeights()
    {
        return _providerWeights.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Resets the predictor.
    /// </summary>
    public void Reset()
    {
        _providerWeights.Clear();
    }

    private Dictionary<string, double> ExtractFeatures(
        RoutingRequest request,
        Provider provider,
        ProviderPerformanceMetrics? metrics)
    {
        var features = new Dictionary<string, double>
        {
            ["request_priority"] = NormalizePriority(request.Priority),
            ["provider_priority"] = NormalizePriority(provider.Priority),
            ["estimated_input_tokens"] = NormalizeTokens(request.EstimatedInputTokens),
            ["estimated_output_tokens"] = NormalizeTokens(request.EstimatedOutputTokens),
            ["max_latency_ms"] = NormalizeLatency(request.MaxLatencyMs),
            ["max_cost"] = NormalizeCost(request.MaxCost),
            ["provider_rate_limit_rpm"] = NormalizeRateLimit(provider.RateLimitRpm)
        };

        if (metrics != null)
        {
            features["avg_latency_ms"] = NormalizeLatency((int)metrics.AverageLatencyMs);
            features["p95_latency_ms"] = NormalizeLatency((int)metrics.P95LatencyMs);
            features["success_rate"] = metrics.SuccessRate / 100.0;
            features["total_requests"] = NormalizeRequestCount(metrics.TotalRequests);
            features["consecutive_failures"] = NormalizeConsecutiveFailures(metrics.ConsecutiveFailures);
            features["avg_cost"] = NormalizeCost(metrics.AverageCostPerRequest);
        }
        else
        {
            // Default values for new providers
            features["avg_latency_ms"] = 0.5; // Medium latency
            features["p95_latency_ms"] = 0.5;
            features["success_rate"] = 1.0; // Assume 100% success
            features["total_requests"] = 0.0;
            features["consecutive_failures"] = 0.0;
            features["avg_cost"] = 0.5;
        }

        return features;
    }

    private double PredictLatency(Dictionary<string, double> features, ProviderPerformanceMetrics? metrics)
    {
        if (metrics != null && metrics.TotalRequests >= _options.MinRequestsForPrediction)
        {
            // Use historical average with some adjustment based on current load
            var baseLatency = metrics.AverageLatencyMs;
            var loadFactor = features.ContainsKey("total_requests") ? features["total_requests"] : 0.0;
            return baseLatency * (1.0 + loadFactor * 0.1);
        }

        // Use heuristic prediction
        return 1000.0 + features["estimated_input_tokens"] * 0.5 + features["estimated_output_tokens"] * 1.0;
    }

    private decimal PredictCost(RoutingRequest request, Provider provider)
    {
        var inputCost = (request.EstimatedInputTokens / 1000.0m) * provider.CostPer1KInputTokens;
        var outputCost = (request.EstimatedOutputTokens / 1000.0m) * provider.CostPer1KOutputTokens;
        return inputCost + outputCost;
    }

    private double PredictSuccessRate(Dictionary<string, double> features, ProviderPerformanceMetrics? metrics)
    {
        if (metrics != null && metrics.TotalRequests >= _options.MinRequestsForPrediction)
        {
            // Use historical success rate with penalty for consecutive failures
            var baseSuccessRate = metrics.SuccessRate / 100.0;
            var failurePenalty = features["consecutive_failures"] * 0.1;
            return Math.Max(0.0, baseSuccessRate - failurePenalty);
        }

        // Default to high success rate for new providers
        return 0.95;
    }

    private double CalculateConfidence(ProviderPerformanceMetrics? metrics)
    {
        if (metrics == null || metrics.TotalRequests < _options.MinRequestsForPrediction)
        {
            return 0.3; // Low confidence for new providers
        }

        // Confidence increases with more data and consistent performance
        var dataConfidence = Math.Min(1.0, metrics.TotalRequests / 100.0);
        var consistencyConfidence = 1.0 - (metrics.P99LatencyMs - metrics.P50LatencyMs) / metrics.P50LatencyMs;
        consistencyConfidence = Math.Max(0.0, consistencyConfidence);

        return (dataConfidence + consistencyConfidence) / 2.0;
    }

    private double CalculateScore(ProviderPrediction prediction, RoutingRequest request)
    {
        var latencyScore = NormalizeScore(prediction.PredictedLatencyMs, 0, request.MaxLatencyMs);
        var costScore = NormalizeScore((double)prediction.PredictedCost, 0, (double)request.MaxCost);
        var successScore = 1.0 - prediction.PredictedSuccessRate; // Invert: lower is better
        var priorityScore = NormalizePriority(prediction.Provider.Priority);

        var weight = _providerWeights.TryGetValue(prediction.Provider.Id, out var w) ? w : 1.0;

        var score = (
            _options.LatencyWeight * latencyScore +
            _options.CostWeight * costScore +
            _options.SuccessRateWeight * successScore +
            _options.PriorityWeight * priorityScore) / weight;

        return score;
    }

    private double NormalizePriority(int priority)
    {
        return Math.Min(1.0, priority / 1000.0);
    }

    private double NormalizeTokens(int tokens)
    {
        return Math.Min(1.0, tokens / 10000.0);
    }

    private double NormalizeLatency(int latencyMs)
    {
        return Math.Min(1.0, latencyMs / 10000.0);
    }

    private double NormalizeCost(decimal cost)
    {
        return Math.Min(1.0, (double)cost / 1.0);
    }

    private double NormalizeRateLimit(int rpm)
    {
        return Math.Min(1.0, rpm / 1000.0);
    }

    private double NormalizeRequestCount(int count)
    {
        return Math.Min(1.0, count / 1000.0);
    }

    private double NormalizeConsecutiveFailures(int failures)
    {
        return Math.Min(1.0, failures / 10.0);
    }

    private double NormalizeScore(double value, double min, double max)
    {
        if (max <= min)
        {
            return 0.5;
        }
        return Math.Max(0.0, Math.Min(1.0, (value - min) / (max - min)));
    }
}

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
