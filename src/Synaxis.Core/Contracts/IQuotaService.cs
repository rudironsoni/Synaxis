using System;
using System.Threading.Tasks;
using Synaxis.Core.Models;

namespace Synaxis.Core.Contracts
{
    /// <summary>
    /// Enforces quota limits for requests and tokens
    /// </summary>
    public interface IQuotaService
    {
        /// <summary>
        /// Check if request is within quota limits
        /// </summary>
        Task<QuotaResult> CheckQuotaAsync(Guid organizationId, QuotaCheckRequest request);
        
        /// <summary>
        /// Check quota for specific user/key
        /// </summary>
        Task<QuotaResult> CheckUserQuotaAsync(Guid organizationId, Guid userId, QuotaCheckRequest request);
        
        /// <summary>
        /// Increment usage counters
        /// </summary>
        Task IncrementUsageAsync(Guid organizationId, UsageMetrics metrics);
        
        /// <summary>
        /// Get current usage for organization
        /// </summary>
        Task<UsageReport> GetUsageAsync(Guid organizationId, UsageQuery query);
        
        /// <summary>
        /// Reset usage counters (e.g., monthly reset)
        /// </summary>
        Task ResetUsageAsync(Guid organizationId, string metricType);
        
        /// <summary>
        /// Get effective limits for organization (plan + overrides)
        /// </summary>
        Task<QuotaLimits> GetEffectiveLimitsAsync(Guid organizationId);
    }
    
    public class QuotaCheckRequest
    {
        public string MetricType { get; set; } // "requests" or "tokens"
        public long IncrementBy { get; set; } = 1;
        public string TimeGranularity { get; set; } // "minute", "hour", "day", "week", "month"
        public WindowType WindowType { get; set; }
    }
    
    public class QuotaResult
    {
        public bool IsAllowed { get; set; }
        public QuotaAction Action { get; set; }
        public string Reason { get; set; }
        public QuotaDetails Details { get; set; }
        public decimal? CreditCharge { get; set; }
        
        public static QuotaResult Allowed() => new() { IsAllowed = true, Action = QuotaAction.Allow };
        public static QuotaResult Throttled(QuotaDetails details) => new() { IsAllowed = false, Action = QuotaAction.Throttle, Details = details };
        public static QuotaResult Blocked(string reason) => new() { IsAllowed = false, Action = QuotaAction.Block, Reason = reason };
        public static QuotaResult Charge(decimal amount) => new() { IsAllowed = true, Action = QuotaAction.CreditCharge, CreditCharge = amount };
    }
    
    public class QuotaDetails
    {
        public string MetricType { get; set; }
        public long Limit { get; set; }
        public long CurrentUsage { get; set; }
        public long Remaining => Limit - CurrentUsage;
        public string TimeWindow { get; set; }
        public DateTime WindowStart { get; set; }
        public DateTime WindowEnd { get; set; }
        public TimeSpan? RetryAfter { get; set; }
    }
    
    public class UsageMetrics
    {
        public Guid? UserId { get; set; }
        public Guid? VirtualKeyId { get; set; }
        public string MetricType { get; set; }
        public long Value { get; set; }
        public string Model { get; set; }
    }
    
    public class UsageQuery
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public string MetricType { get; set; }
        public string Granularity { get; set; } // "hour", "day", "week", "month"
    }
    
    public class UsageReport
    {
        public Guid OrganizationId { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public Dictionary<string, long> UsageByMetric { get; set; }
        public Dictionary<string, long> UsageByModel { get; set; }
        public decimal TotalCost { get; set; }
    }
    
    public class QuotaLimits
    {
        public int MaxConcurrentRequests { get; set; }
        public long MonthlyRequestLimit { get; set; }
        public long MonthlyTokenLimit { get; set; }
        public int RequestsPerMinute { get; set; }
        public int TokensPerMinute { get; set; }
    }
    
    public enum QuotaAction
    {
        Allow,
        Throttle,
        Block,
        CreditCharge
    }
    
    public enum WindowType
    {
        Fixed,
        Sliding
    }
}
