using System.Collections.Concurrent;

namespace Synaplexer.Core.Metrics;

/// <summary>
/// Tracks success/failure rates for providers and access methods.
/// </summary>
public class MetricsCollector : IMetricsCollector
{
    private readonly ConcurrentDictionary<string, ProviderMetrics> _providerMetrics = new();
    private readonly ConcurrentDictionary<string, MethodMetrics> _methodMetrics = new();

    public void RecordSuccess(string provider, AccessMethodType method, TimeSpan duration)
    {
        var providerKey = provider.ToLowerInvariant();
        var methodKey = $"{providerKey}_{method}";

        _providerMetrics.AddOrUpdate(providerKey,
            new ProviderMetrics { Provider = provider },
            (_, existing) =>
            {
                existing.TotalRequests++;
                existing.SuccessfulRequests++;
                existing.TotalDuration += duration;
                existing.LastSuccessTime = DateTime.UtcNow;
                return existing;
            });

        _methodMetrics.AddOrUpdate(methodKey,
            new MethodMetrics { Provider = provider, Method = method },
            (_, existing) =>
            {
                existing.TotalRequests++;
                existing.SuccessfulRequests++;
                existing.TotalDuration += duration;
                return existing;
            });
    }

    public void RecordFailure(string provider, AccessMethodType method, string error, TimeSpan duration)
    {
        var providerKey = provider.ToLowerInvariant();
        var methodKey = $"{providerKey}_{method}";

        _providerMetrics.AddOrUpdate(providerKey,
            new ProviderMetrics { Provider = provider },
            (_, existing) =>
            {
                existing.TotalRequests++;
                existing.FailedRequests++;
                existing.LastFailureTime = DateTime.UtcNow;
                existing.LastError = error;
                return existing;
            });

        _methodMetrics.AddOrUpdate(methodKey,
            new MethodMetrics { Provider = provider, Method = method },
            (_, existing) =>
            {
                existing.TotalRequests++;
                existing.FailedRequests++;
                existing.LastFailureTime = DateTime.UtcNow;
                existing.LastError = error;
                return existing;
            });
    }

    public ProviderMetrics? GetProviderMetrics(string provider)
    {
        _providerMetrics.TryGetValue(provider.ToLowerInvariant(), out var metrics);
        return metrics;
    }

    public MethodMetrics? GetMethodMetrics(string provider, AccessMethodType method)
    {
        var methodKey = $"{provider.ToLowerInvariant()}_{method}";
        _methodMetrics.TryGetValue(methodKey, out var metrics);
        return metrics;
    }

    public IEnumerable<ProviderMetrics> GetAllProviderMetrics()
    {
        return _providerMetrics.Values.ToList();
    }

    public IEnumerable<MethodMetrics> GetAllMethodMetrics()
    {
        return _methodMetrics.Values.ToList();
    }

    public double GetSuccessRate(string provider, AccessMethodType? method = null)
    {
        if (method.HasValue)
        {
            var metrics = GetMethodMetrics(provider, method.Value);
            return metrics?.SuccessRate ?? 0;
        }

        var providerMetrics = GetProviderMetrics(provider);
        return providerMetrics?.SuccessRate ?? 0;
    }

    public TimeSpan GetAverageDuration(string provider, AccessMethodType? method = null)
    {
        if (method.HasValue)
        {
            var metrics = GetMethodMetrics(provider, method.Value);
            return metrics?.AverageDuration ?? TimeSpan.Zero;
        }

        var providerMetrics = GetProviderMetrics(provider);
        return providerMetrics?.AverageDuration ?? TimeSpan.Zero;
    }

    public void Clear()
    {
        _providerMetrics.Clear();
        _methodMetrics.Clear();
    }
}

/// <summary>
/// Metrics for a specific provider.
/// </summary>
public class ProviderMetrics
{
    public string Provider { get; set; } = string.Empty;
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public DateTime? LastSuccessTime { get; set; }
    public DateTime? LastFailureTime { get; set; }
    public string? LastError { get; set; }

    public double SuccessRate => TotalRequests > 0
        ? (double)SuccessfulRequests / TotalRequests
        : 0;

    public TimeSpan AverageDuration => TotalRequests > 0
        ? TimeSpan.FromTicks(TotalDuration.Ticks / TotalRequests)
        : TimeSpan.Zero;
}

/// <summary>
/// Metrics for a specific access method within a provider.
/// </summary>
public class MethodMetrics
{
    public string Provider { get; set; } = string.Empty;
    public AccessMethodType Method { get; set; }
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public DateTime? LastSuccessTime { get; set; }
    public DateTime? LastFailureTime { get; set; }
    public string? LastError { get; set; }

    public double SuccessRate => TotalRequests > 0
        ? (double)SuccessfulRequests / TotalRequests
        : 0;

    public TimeSpan AverageDuration => TotalRequests > 0
        ? TimeSpan.FromTicks(TotalDuration.Ticks / TotalRequests)
        : TimeSpan.Zero;
}

/// <summary>
/// Interface for metrics collection to allow for alternative implementations.
/// </summary>
public interface IMetricsCollector
{
    void RecordSuccess(string provider, AccessMethodType method, TimeSpan duration);
    void RecordFailure(string provider, AccessMethodType method, string error, TimeSpan duration);
    ProviderMetrics? GetProviderMetrics(string provider);
    MethodMetrics? GetMethodMetrics(string provider, AccessMethodType method);
    double GetSuccessRate(string provider, AccessMethodType? method = null);
    TimeSpan GetAverageDuration(string provider, AccessMethodType? method = null);
    IEnumerable<ProviderMetrics> GetAllProviderMetrics();
    void Clear();
}
