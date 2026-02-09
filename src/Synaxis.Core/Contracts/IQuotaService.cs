// <copyright file="IQuotaService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Synaxis.Core.Models;

    /// <summary>
    /// Enforces quota limits for requests and tokens.
    /// </summary>
    public interface IQuotaService
    {
        /// <summary>
        /// Check if request is within quota limits.
        /// </summary>
        /// <param name="organizationId">The unique identifier of the organization.</param>
        /// <param name="request">The quota check request parameters.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the quota check result.</returns>
        Task<QuotaResult> CheckQuotaAsync(Guid organizationId, QuotaCheckRequest request);

        /// <summary>
        /// Check quota for specific user/key.
        /// </summary>
        /// <param name="organizationId">The unique identifier of the organization.</param>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="request">The quota check request parameters.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the quota check result.</returns>
        Task<QuotaResult> CheckUserQuotaAsync(Guid organizationId, Guid userId, QuotaCheckRequest request);

        /// <summary>
        /// Increment usage counters.
        /// </summary>
        /// <param name="organizationId">The unique identifier of the organization.</param>
        /// <param name="metrics">The usage metrics to record.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task IncrementUsageAsync(Guid organizationId, UsageMetrics metrics);

        /// <summary>
        /// Get current usage for organization.
        /// </summary>
        /// <param name="organizationId">The unique identifier of the organization.</param>
        /// <param name="query">The usage query parameters.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the usage report.</returns>
        Task<UsageReport> GetUsageAsync(Guid organizationId, UsageQuery query);

        /// <summary>
        /// Reset usage counters (e.g., monthly reset).
        /// </summary>
        /// <param name="organizationId">The unique identifier of the organization.</param>
        /// <param name="metricType">The type of metric to reset.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task ResetUsageAsync(Guid organizationId, string metricType);

        /// <summary>
        /// Get effective limits for organization (plan + overrides).
        /// </summary>
        /// <param name="organizationId">The unique identifier of the organization.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the quota limits.</returns>
        Task<QuotaLimits> GetEffectiveLimitsAsync(Guid organizationId);
    }

    /// <summary>
    /// Represents a request to check quota limits.
    /// </summary>
    public class QuotaCheckRequest
    {
        /// <summary>
        /// Gets or sets the metric type (requests or tokens).
        /// </summary>
        public string MetricType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the amount to increment by.
        /// </summary>
        public long IncrementBy { get; set; } = 1;

        /// <summary>
        /// Gets or sets the time granularity (minute, hour, day, week, month).
        /// </summary>
        public string TimeGranularity { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the window type.
        /// </summary>
        public WindowType WindowType { get; set; }
    }

    /// <summary>
    /// Represents the result of a quota check.
    /// </summary>
    public class QuotaResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the request is allowed.
        /// </summary>
        public bool IsAllowed { get; set; }

        /// <summary>
        /// Gets or sets the quota action to take.
        /// </summary>
        public QuotaAction Action { get; set; }

        /// <summary>
        /// Gets or sets the reason for the action.
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the quota details.
        /// </summary>
        public QuotaDetails? Details { get; set; }

        /// <summary>
        /// Gets or sets the credit charge amount.
        /// </summary>
        public decimal? CreditCharge { get; set; }

        /// <summary>
        /// Creates an allowed quota result.
        /// </summary>
        /// <returns>A quota result indicating request is allowed.</returns>
        public static QuotaResult Allowed() => new() { IsAllowed = true, Action = QuotaAction.Allow };

        /// <summary>
        /// Creates a throttled quota result.
        /// </summary>
        /// <param name="details">The quota details.</param>
        /// <returns>A quota result indicating request is throttled.</returns>
        public static QuotaResult Throttled(QuotaDetails details) => new() { IsAllowed = false, Action = QuotaAction.Throttle, Details = details };

        /// <summary>
        /// Creates a blocked quota result.
        /// </summary>
        /// <param name="reason">The reason for blocking.</param>
        /// <returns>A quota result indicating request is blocked.</returns>
        public static QuotaResult Blocked(string reason) => new() { IsAllowed = false, Action = QuotaAction.Block, Reason = reason };

        /// <summary>
        /// Creates a credit charge quota result.
        /// </summary>
        /// <param name="amount">The amount to charge.</param>
        /// <returns>A quota result indicating credit charge is required.</returns>
        public static QuotaResult Charge(decimal amount) => new() { IsAllowed = true, Action = QuotaAction.CreditCharge, CreditCharge = amount };
    }

    /// <summary>
    /// Represents detailed quota information.
    /// </summary>
    public class QuotaDetails
    {
        /// <summary>
        /// Gets or sets the metric type.
        /// </summary>
        public string MetricType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the quota limit.
        /// </summary>
        public long Limit { get; set; }

        /// <summary>
        /// Gets or sets the current usage.
        /// </summary>
        public long CurrentUsage { get; set; }

        /// <summary>
        /// Gets the remaining quota.
        /// </summary>
        public long Remaining => this.Limit - this.CurrentUsage;

        /// <summary>
        /// Gets or sets the time window description.
        /// </summary>
        public string TimeWindow { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the window start time.
        /// </summary>
        public DateTime WindowStart { get; set; }

        /// <summary>
        /// Gets or sets the window end time.
        /// </summary>
        public DateTime WindowEnd { get; set; }

        /// <summary>
        /// Gets or sets the retry after duration.
        /// </summary>
        public TimeSpan? RetryAfter { get; set; }
    }

    /// <summary>
    /// Represents usage metrics to record.
    /// </summary>
    public class UsageMetrics
    {
        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the virtual key identifier.
        /// </summary>
        public Guid? VirtualKeyId { get; set; }

        /// <summary>
        /// Gets or sets the metric type.
        /// </summary>
        public string MetricType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the metric value.
        /// </summary>
        public long Value { get; set; }

        /// <summary>
        /// Gets or sets the model used.
        /// </summary>
        public string Model { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a query for usage data.
    /// </summary>
    public class UsageQuery
    {
        /// <summary>
        /// Gets or sets the start date.
        /// </summary>
        public DateTime From { get; set; }

        /// <summary>
        /// Gets or sets the end date.
        /// </summary>
        public DateTime To { get; set; }

        /// <summary>
        /// Gets or sets the metric type filter.
        /// </summary>
        public string MetricType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the granularity (hour, day, week, month).
        /// </summary>
        public string Granularity { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a usage report.
    /// </summary>
    public class UsageReport
    {
        /// <summary>
        /// Gets or sets the organization identifier.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the report start date.
        /// </summary>
        public DateTime From { get; set; }

        /// <summary>
        /// Gets or sets the report end date.
        /// </summary>
        public DateTime To { get; set; }

        /// <summary>
        /// Gets or sets usage grouped by metric.
        /// </summary>
        public IDictionary<string, long> UsageByMetric { get; set; } = new Dictionary<string, long>(StringComparer.Ordinal);

        /// <summary>
        /// Gets or sets usage grouped by model.
        /// </summary>
        public IDictionary<string, long> UsageByModel { get; set; } = new Dictionary<string, long>(StringComparer.Ordinal);

        /// <summary>
        /// Gets or sets the total cost.
        /// </summary>
        public decimal TotalCost { get; set; }
    }

    /// <summary>
    /// Represents quota limits for an organization.
    /// </summary>
    public class QuotaLimits
    {
        /// <summary>
        /// Gets or sets the maximum concurrent requests.
        /// </summary>
        public int MaxConcurrentRequests { get; set; }

        /// <summary>
        /// Gets or sets the monthly request limit.
        /// </summary>
        public long MonthlyRequestLimit { get; set; }

        /// <summary>
        /// Gets or sets the monthly token limit.
        /// </summary>
        public long MonthlyTokenLimit { get; set; }

        /// <summary>
        /// Gets or sets the requests per minute limit.
        /// </summary>
        public int RequestsPerMinute { get; set; }

        /// <summary>
        /// Gets or sets the tokens per minute limit.
        /// </summary>
        public int TokensPerMinute { get; set; }
    }

    /// <summary>
    /// Represents the action to take when quota is checked.
    /// </summary>
    public enum QuotaAction
    {
        /// <summary>
        /// Allow the request.
        /// </summary>
        Allow,

        /// <summary>
        /// Throttle the request.
        /// </summary>
        Throttle,

        /// <summary>
        /// Block the request.
        /// </summary>
        Block,

        /// <summary>
        /// Charge credits for the request.
        /// </summary>
        CreditCharge,
    }

    /// <summary>
    /// Represents the type of time window for quota enforcement.
    /// </summary>
    public enum WindowType
    {
        /// <summary>
        /// Fixed time window.
        /// </summary>
        Fixed,

        /// <summary>
        /// Sliding time window.
        /// </summary>
        Sliding,
    }
}
