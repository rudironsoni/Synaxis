using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Synaplexer.Core.Metrics
{
    /// <summary>
    /// Tracks usage metrics for LLM providers including tokens, costs, and request counts.
    /// </summary>
    public class UsageTracker
    {
        private readonly ConcurrentDictionary<string, ProviderUsage> _usage = new();
        private readonly ConcurrentDictionary<string, ModelPricing> _pricing = new();
        private readonly ILogger<UsageTracker> _logger;

        public UsageTracker(ILogger<UsageTracker> logger)
        {
            _logger = logger;
            InitializeDefaultPricing();
        }

        /// <summary>
        /// Records a completed request with token usage.
        /// </summary>
        public void RecordRequest(
            string provider,
            string model,
            int promptTokens,
            int completionTokens,
            int reasoningTokens = 0,
            bool success = true,
            TimeSpan? latency = null)
        {
            var key = GetKey(provider, model);
            var usage = _usage.GetOrAdd(key, _ => new ProviderUsage
            {
                Provider = provider,
                Model = model
            });

            lock (usage)
            {
                usage.TotalRequests++;
                usage.TotalPromptTokens += promptTokens;
                usage.TotalCompletionTokens += completionTokens;
                usage.TotalReasoningTokens += reasoningTokens;
                usage.LastUsed = DateTime.UtcNow;

                if (success)
                {
                    usage.SuccessfulRequests++;
                }
                else
                {
                    usage.FailedRequests++;
                }

                if (latency.HasValue)
                {
                    usage.TotalLatencyMs += (long)latency.Value.TotalMilliseconds;
                }

                // Calculate cost
                var cost = CalculateCost(provider, model, promptTokens, completionTokens, reasoningTokens);
                usage.TotalCost += cost;

                // Track daily usage
                var today = DateTime.UtcNow.Date;
                if (!usage.DailyUsage.ContainsKey(today))
                {
                    usage.DailyUsage[today] = new DailyUsage();
                }
                usage.DailyUsage[today].Requests++;
                usage.DailyUsage[today].Tokens += promptTokens + completionTokens + reasoningTokens;
                usage.DailyUsage[today].Cost += cost;
            }

            _logger.LogDebug(
                "Recorded request for {Provider}/{Model}: {Prompt}+{Completion}+{Reasoning} tokens, ${Cost:F6}",
                provider, model, promptTokens, completionTokens, reasoningTokens, 
                CalculateCost(provider, model, promptTokens, completionTokens, reasoningTokens));
        }

        /// <summary>
        /// Records a rate limit hit.
        /// </summary>
        public void RecordRateLimitHit(string provider, string model)
        {
            var key = GetKey(provider, model);
            var usage = _usage.GetOrAdd(key, _ => new ProviderUsage
            {
                Provider = provider,
                Model = model
            });

            lock (usage)
            {
                usage.RateLimitedCount++;
            }
        }

        /// <summary>
        /// Gets usage for a specific provider/model combination.
        /// </summary>
        public ProviderUsage? GetUsage(string provider, string model)
        {
            var key = GetKey(provider, model);
            return _usage.TryGetValue(key, out var usage) ? usage : null;
        }

        /// <summary>
        /// Gets aggregated usage for a provider across all models.
        /// </summary>
        public ProviderUsage GetProviderUsage(string provider)
        {
            var aggregate = new ProviderUsage { Provider = provider, Model = "*" };

            foreach (var kvp in _usage.Where(u => u.Value.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase)))
            {
                lock (kvp.Value)
                {
                    aggregate.TotalRequests += kvp.Value.TotalRequests;
                    aggregate.SuccessfulRequests += kvp.Value.SuccessfulRequests;
                    aggregate.FailedRequests += kvp.Value.FailedRequests;
                    aggregate.TotalPromptTokens += kvp.Value.TotalPromptTokens;
                    aggregate.TotalCompletionTokens += kvp.Value.TotalCompletionTokens;
                    aggregate.TotalReasoningTokens += kvp.Value.TotalReasoningTokens;
                    aggregate.TotalCost += kvp.Value.TotalCost;
                    aggregate.TotalLatencyMs += kvp.Value.TotalLatencyMs;
                    aggregate.RateLimitedCount += kvp.Value.RateLimitedCount;

                    if (kvp.Value.LastUsed > aggregate.LastUsed)
                    {
                        aggregate.LastUsed = kvp.Value.LastUsed;
                    }
                }
            }

            return aggregate;
        }

        /// <summary>
        /// Generates a comprehensive usage report.
        /// </summary>
        public UsageReport GetReport(DateTime? from = null, DateTime? to = null)
        {
            var report = new UsageReport
            {
                GeneratedAt = DateTime.UtcNow,
                PeriodStart = from ?? DateTime.MinValue,
                PeriodEnd = to ?? DateTime.UtcNow
            };

            foreach (var kvp in _usage)
            {
                lock (kvp.Value)
                {
                    if (from.HasValue && kvp.Value.LastUsed < from.Value) continue;
                    if (to.HasValue && kvp.Value.LastUsed > to.Value) continue;

                    report.ProviderUsage[kvp.Key] = CloneUsage(kvp.Value);
                    report.TotalCost += kvp.Value.TotalCost;
                    report.TotalTokens += kvp.Value.TotalPromptTokens +
                                          kvp.Value.TotalCompletionTokens +
                                          kvp.Value.TotalReasoningTokens;
                    report.TotalRequests += kvp.Value.TotalRequests;
                }
            }

            // Group by provider
            foreach (var provider in _usage.Values.Select(u => u.Provider).Distinct())
            {
                report.ByProvider[provider] = GetProviderUsage(provider);
            }

            return report;
        }

        /// <summary>
        /// Gets usage for today.
        /// </summary>
        public UsageReport GetTodayReport()
        {
            var today = DateTime.UtcNow.Date;
            return GetReport(today, today.AddDays(1));
        }

        /// <summary>
        /// Configures pricing for a provider/model.
        /// </summary>
        public void SetPricing(string provider, string model, decimal inputPricePerMillion, decimal outputPricePerMillion, decimal? reasoningPricePerMillion = null)
        {
            var key = GetKey(provider, model);
            _pricing[key] = new ModelPricing
            {
                InputPricePerMillion = inputPricePerMillion,
                OutputPricePerMillion = outputPricePerMillion,
                ReasoningPricePerMillion = reasoningPricePerMillion ?? outputPricePerMillion
            };
        }

        /// <summary>
        /// Resets all usage data.
        /// </summary>
        public void Reset()
        {
            _usage.Clear();
            _logger.LogInformation("Reset all usage data");
        }

        /// <summary>
        /// Resets usage for a specific provider.
        /// </summary>
        public void Reset(string provider)
        {
            var keysToRemove = _usage.Keys.Where(k => k.StartsWith(provider + ":", StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var key in keysToRemove)
            {
                _usage.TryRemove(key, out _);
            }
            _logger.LogInformation("Reset usage data for {Provider}", provider);
        }

        private decimal CalculateCost(string provider, string model, int promptTokens, int completionTokens, int reasoningTokens)
        {
            var key = GetKey(provider, model);
            
            // Try exact match first
            if (!_pricing.TryGetValue(key, out var pricing))
            {
                // Try provider-level pricing
                var providerKey = GetKey(provider, "*");
                if (!_pricing.TryGetValue(providerKey, out pricing))
                {
                    return 0; // Free tier or unknown pricing
                }
            }

            var inputCost = (promptTokens / 1_000_000m) * pricing.InputPricePerMillion;
            var outputCost = (completionTokens / 1_000_000m) * pricing.OutputPricePerMillion;
            var reasoningCost = (reasoningTokens / 1_000_000m) * pricing.ReasoningPricePerMillion;

            return inputCost + outputCost + reasoningCost;
        }

        private void InitializeDefaultPricing()
        {
            // DeepInfra (very cheap)
            SetPricing("deepinfra", "*", 0.04m, 0.04m);
            SetPricing("deepinfra", "DeepSeek-R1", 0.55m, 2.19m, 2.19m);
            SetPricing("deepinfra", "DeepSeek-V3", 0.14m, 0.28m);

            // Together AI
            SetPricing("togetherai", "*", 0.10m, 0.10m);

            // xAI (Grok)
            SetPricing("xai", "grok-3", 2.00m, 10.00m);
            SetPricing("xai", "grok-3-beta", 2.00m, 10.00m);
            SetPricing("xai", "grok-2", 2.00m, 10.00m);

            // OpenRouter (varies by model)
            SetPricing("openrouter", "*", 0m, 0m); // Free tier models
            SetPricing("openrouter", "hermes-4-405b", 2.70m, 2.70m);
            SetPricing("openrouter", "deepseek-r1:free", 0m, 0m);

            // Anthropic
            SetPricing("anthropic", "claude-3-5-sonnet", 3.00m, 15.00m);
            SetPricing("anthropic", "claude-3-opus", 15.00m, 75.00m);
            SetPricing("anthropic", "claude-3-haiku", 0.25m, 1.25m);

            // Cohere
            SetPricing("cohere", "*", 0.15m, 0.60m);

            // NVIDIA
            SetPricing("nvidia", "*", 0m, 0m); // Free tier

            // Replicate (pay per second, approximated)
            SetPricing("replicate", "*", 0.05m, 0.10m);

            // Free providers
            SetPricing("pollinations", "*", 0m, 0m);
            SetPricing("cloudflare", "*", 0m, 0m);
            SetPricing("lambdachat", "*", 0m, 0m);
        }

        private static string GetKey(string provider, string model) =>
            $"{provider.ToLowerInvariant()}:{model.ToLowerInvariant()}";

        private static ProviderUsage CloneUsage(ProviderUsage source) => new()
        {
            Provider = source.Provider,
            Model = source.Model,
            TotalRequests = source.TotalRequests,
            SuccessfulRequests = source.SuccessfulRequests,
            FailedRequests = source.FailedRequests,
            TotalPromptTokens = source.TotalPromptTokens,
            TotalCompletionTokens = source.TotalCompletionTokens,
            TotalReasoningTokens = source.TotalReasoningTokens,
            TotalCost = source.TotalCost,
            TotalLatencyMs = source.TotalLatencyMs,
            RateLimitedCount = source.RateLimitedCount,
            LastUsed = source.LastUsed,
            DailyUsage = new Dictionary<DateTime, DailyUsage>(source.DailyUsage)
        };
    }

    /// <summary>
    /// Usage statistics for a provider/model combination.
    /// </summary>
    public class ProviderUsage
    {
        public string Provider { get; set; } = "";
        public string Model { get; set; } = "";
        public long TotalRequests { get; set; }
        public long SuccessfulRequests { get; set; }
        public long FailedRequests { get; set; }
        public long TotalPromptTokens { get; set; }
        public long TotalCompletionTokens { get; set; }
        public long TotalReasoningTokens { get; set; }
        public decimal TotalCost { get; set; }
        public long TotalLatencyMs { get; set; }
        public int RateLimitedCount { get; set; }
        public DateTime LastUsed { get; set; }
        public Dictionary<DateTime, DailyUsage> DailyUsage { get; set; } = new();

        public long TotalTokens => TotalPromptTokens + TotalCompletionTokens + TotalReasoningTokens;
        public double AverageLatencyMs => TotalRequests > 0 ? TotalLatencyMs / (double)TotalRequests : 0;
        public double SuccessRate => TotalRequests > 0 ? SuccessfulRequests / (double)TotalRequests : 0;
    }

    /// <summary>
    /// Daily usage breakdown.
    /// </summary>
    public class DailyUsage
    {
        public long Requests { get; set; }
        public long Tokens { get; set; }
        public decimal Cost { get; set; }
    }

    /// <summary>
    /// Comprehensive usage report.
    /// </summary>
    public class UsageReport
    {
        public DateTime GeneratedAt { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public Dictionary<string, ProviderUsage> ProviderUsage { get; set; } = new();
        public Dictionary<string, ProviderUsage> ByProvider { get; set; } = new();
        public decimal TotalCost { get; set; }
        public long TotalTokens { get; set; }
        public long TotalRequests { get; set; }
    }

    /// <summary>
    /// Pricing information for a model.
    /// </summary>
    internal class ModelPricing
    {
        public decimal InputPricePerMillion { get; set; }
        public decimal OutputPricePerMillion { get; set; }
        public decimal ReasoningPricePerMillion { get; set; }
    }
}
