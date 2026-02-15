// <copyright file="QuotaService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using StackExchange.Redis;
    using Synaxis.Core.Contracts;
    using Synaxis.Core.Models;
    using Synaxis.Infrastructure.Data;

    /// <summary>
    /// Enforces quota limits for requests and tokens with Redis-based distributed state.
    /// </summary>
    public class QuotaService : IQuotaService
    {
        private readonly SynaxisDbContext _context;
        private readonly IConnectionMultiplexer _redis;
        private readonly ITenantService _tenantService;
        private readonly ILogger<QuotaService> _logger;

        // Lua script for atomic quota check with both fixed and sliding window support
        private const string LuaCheckQuotaFixed = @"
            local key = KEYS[1]
            local limit = tonumber(ARGV[1])
            local window_seconds = tonumber(ARGV[2])
            local increment = tonumber(ARGV[3])
            
            local current = redis.call('GET', key)
            
            if current == false then
                redis.call('SET', key, increment, 'EX', window_seconds)
                return {increment, window_seconds, 'new'}
            end
            
            current = tonumber(current)
            
            if current + increment > limit then
                local ttl = redis.call('TTL', key)
                return {current, ttl, 'exceeded'}
            end
            
            local new_count = redis.call('INCRBY', key, increment)
            local ttl = redis.call('TTL', key)
            return {new_count, ttl, 'allowed'}
        ";

        // Lua script for sliding window using sorted sets
        private const string LuaCheckQuotaSliding = @"
            local key = KEYS[1]
            local limit = tonumber(ARGV[1])
            local window_seconds = tonumber(ARGV[2])
            local now = tonumber(ARGV[3])
            local increment = tonumber(ARGV[4])
            
            -- Remove expired entries
            local cutoff = now - window_seconds
            redis.call('ZREMRANGEBYSCORE', key, '-inf', cutoff)
            
            -- Count current usage
            local current = redis.call('ZCARD', key)
            
            if current + increment > limit then
                -- Find time until oldest entry expires
                local oldest = redis.call('ZRANGE', key, 0, 0, 'WITHSCORES')
                local retry_after = window_seconds
                if #oldest > 0 then
                    retry_after = math.ceil(tonumber(oldest[2]) + window_seconds - now)
                end
                return {current, retry_after, 'exceeded'}
            end
            
            -- Add new entries with current timestamp
            for i = 1, increment do
                redis.call('ZADD', key, now, now .. ':' .. i)
            end
            
            -- Set expiry on the entire key
            redis.call('EXPIRE', key, window_seconds)
            
            local new_count = redis.call('ZCARD', key)
            return {new_count, window_seconds, 'allowed'}
        ";

        /// <summary>
        /// Initializes a new instance of the <see cref="QuotaService"/> class.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="redis"></param>
        /// <param name="tenantService"></param>
        /// <param name="logger"></param>
        public QuotaService(
            SynaxisDbContext context,
            IConnectionMultiplexer redis,
            ITenantService tenantService,
            ILogger<QuotaService> logger)
        {
            this._context = context ?? throw new ArgumentNullException(nameof(context));
            this._redis = redis ?? throw new ArgumentNullException(nameof(redis));
            this._tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<QuotaResult> CheckQuotaAsync(Guid organizationId, QuotaCheckRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            try
            {
                var limits = await this.GetEffectiveLimitsAsync(organizationId).ConfigureAwait(false);

                // Check concurrent requests first
                if (string.Equals(request.MetricType, "requests", StringComparison.Ordinal))
                {
                    var concurrentCheck = await this.CheckConcurrentLimitAsync(organizationId, limits).ConfigureAwait(false);
                    if (!concurrentCheck.IsAllowed)
                    {
                        return concurrentCheck;
                    }
                }

                // Determine the limit to apply
                long limit = this.DetermineLimit(request, limits);

                if (limit <= 0)
                {
                    // No limit configured, allow request
                    return QuotaResult.Allowed();
                }

                // Check quota using Redis
                var db = this._redis.GetDatabase();
                var key = this.BuildQuotaKey(organizationId, null, null, request.MetricType, request.TimeGranularity);
                var windowSeconds = this.GetWindowSeconds(request.TimeGranularity);

                RedisResult result;
                if (request.WindowType == WindowType.Fixed)
                {
                    result = await db.ScriptEvaluateAsync(
                        LuaCheckQuotaFixed,
                        new RedisKey[] { key },
                        new RedisValue[] { limit, windowSeconds, request.IncrementBy }).ConfigureAwait(false);
                }
                else
                {
                    var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    result = await db.ScriptEvaluateAsync(
                        LuaCheckQuotaSliding,
                        new RedisKey[] { key },
                        new RedisValue[] { limit, windowSeconds, now, request.IncrementBy }).ConfigureAwait(false);
                }

                var values = (RedisValue[])result;
                var current = (long)values[0];
                var ttl = (long)values[1];
                var status = (string)values[2];

                if (string.Equals(status, "exceeded", StringComparison.Ordinal))
                {
                    this._logger.LogWarning(
                        "Quota exceeded for org {OrgId}, metric {Metric}: {Current}/{Limit}",
                        organizationId, request.MetricType, current, limit);

                    var details = new QuotaDetails
                    {
                        MetricType = request.MetricType,
                        Limit = limit,
                        CurrentUsage = current,
                        TimeWindow = request.TimeGranularity,
                        WindowStart = this.CalculateWindowStart(request.TimeGranularity, request.WindowType),
                        WindowEnd = DateTime.UtcNow.AddSeconds(ttl),
                        RetryAfter = TimeSpan.FromSeconds(ttl),
                    };

                    return QuotaResult.Throttled(details);
                }

                this._logger.LogDebug(
                    "Quota check passed for org {OrgId}, metric {Metric}: {Current}/{Limit}",
                    organizationId, request.MetricType, current, limit);

                return QuotaResult.Allowed();
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error checking quota for org {OrgId}", organizationId);

                // Fail open: allow request if quota check fails
                return QuotaResult.Allowed();
            }
        }

        /// <inheritdoc/>
        public async Task<QuotaResult> CheckUserQuotaAsync(Guid organizationId, Guid userId, QuotaCheckRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            try
            {
                var limits = await this.GetEffectiveLimitsAsync(organizationId).ConfigureAwait(false);
                long limit = this.DetermineLimit(request, limits);

                if (limit <= 0)
                {
                    return QuotaResult.Allowed();
                }

                var db = this._redis.GetDatabase();
                var key = this.BuildQuotaKey(organizationId, userId, null, request.MetricType, request.TimeGranularity);
                var windowSeconds = this.GetWindowSeconds(request.TimeGranularity);

                RedisResult result;
                if (request.WindowType == WindowType.Fixed)
                {
                    result = await db.ScriptEvaluateAsync(
                        LuaCheckQuotaFixed,
                        new RedisKey[] { key },
                        new RedisValue[] { limit, windowSeconds, request.IncrementBy }).ConfigureAwait(false);
                }
                else
                {
                    var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    result = await db.ScriptEvaluateAsync(
                        LuaCheckQuotaSliding,
                        new RedisKey[] { key },
                        new RedisValue[] { limit, windowSeconds, now, request.IncrementBy }).ConfigureAwait(false);
                }

                var values = (RedisValue[])result;
                var current = (long)values[0];
                var ttl = (long)values[1];
                var status = (string)values[2];

                if (string.Equals(status, "exceeded", StringComparison.Ordinal))
                {
                    var details = new QuotaDetails
                    {
                        MetricType = request.MetricType,
                        Limit = limit,
                        CurrentUsage = current,
                        TimeWindow = request.TimeGranularity,
                        WindowStart = this.CalculateWindowStart(request.TimeGranularity, request.WindowType),
                        WindowEnd = DateTime.UtcNow.AddSeconds(ttl),
                        RetryAfter = TimeSpan.FromSeconds(ttl),
                    };

                    return QuotaResult.Throttled(details);
                }

                return QuotaResult.Allowed();
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error checking user quota for org {OrgId}, user {UserId}", organizationId, userId);
                return QuotaResult.Allowed();
            }
        }

        /// <inheritdoc/>
        public async Task IncrementUsageAsync(Guid organizationId, UsageMetrics metrics)
        {
            if (metrics == null)
            {
                throw new ArgumentNullException(nameof(metrics));
            }

            try
            {
                var db = this._redis.GetDatabase();

                // Increment monthly counters
                var monthKey = this.BuildQuotaKey(organizationId, metrics.UserId, metrics.VirtualKeyId, metrics.MetricType, "month");
                await db.StringIncrementAsync(monthKey, metrics.Value).ConfigureAwait(false);
                await db.KeyExpireAsync(monthKey, TimeSpan.FromDays(31)).ConfigureAwait(false);

                // Increment daily counters for analytics
                var dayKey = this.BuildQuotaKey(organizationId, metrics.UserId, metrics.VirtualKeyId, metrics.MetricType, "day");
                await db.StringIncrementAsync(dayKey, metrics.Value).ConfigureAwait(false);
                await db.KeyExpireAsync(dayKey, TimeSpan.FromDays(2)).ConfigureAwait(false);

                this._logger.LogDebug(
                    "Incremented usage for org {OrgId}, metric {Metric}: +{Value}",
                    organizationId, metrics.MetricType, metrics.Value);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error incrementing usage for org {OrgId}", organizationId);
            }
        }

        /// <inheritdoc/>
        public async Task<UsageReport> GetUsageAsync(Guid organizationId, UsageQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            try
            {
                var db = this._redis.GetDatabase();
                var report = new UsageReport
                {
                    OrganizationId = organizationId,
                    From = query.From,
                    To = query.To,
                    UsageByMetric = new Dictionary<string, long>(StringComparer.Ordinal),
                    UsageByModel = new Dictionary<string, long>(StringComparer.Ordinal),
                };

                // Get usage from Redis for current period
                var metricTypes = string.IsNullOrEmpty(query.MetricType)
                    ? new[] { "requests", "tokens" }
                    : new[] { query.MetricType };

                foreach (var metricType in metricTypes)
                {
                    var key = this.BuildQuotaKey(organizationId, null, null, metricType, query.Granularity ?? "month");
                    var value = await db.StringGetAsync(key).ConfigureAwait(false);

                    if (value.HasValue)
                    {
                        report.UsageByMetric[metricType] = (long)value;
                    }
                    else
                    {
                        report.UsageByMetric[metricType] = 0;
                    }
                }

                return report;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error getting usage for org {OrgId}", organizationId);
                return new UsageReport
                {
                    OrganizationId = organizationId,
                    From = query.From,
                    To = query.To,
                    UsageByMetric = new Dictionary<string, long>(StringComparer.Ordinal),
                    UsageByModel = new Dictionary<string, long>(StringComparer.Ordinal),
                };
            }
        }

        /// <inheritdoc/>
        public async Task ResetUsageAsync(Guid organizationId, string metricType)
        {
            try
            {
                var db = this._redis.GetDatabase();
                var pattern = $"quota:org:{organizationId}:*:{metricType}:*";

                // Note: In production, use Redis SCAN instead of KEYS for better performance
                var endpoints = this._redis.GetEndPoints();
                var server = this._redis.GetServer(endpoints.First());

                await foreach (var key in server.KeysAsync(pattern: pattern).ConfigureAwait(false))
                {
                    await db.KeyDeleteAsync(key).ConfigureAwait(false);
                }

                this._logger.LogInformation("Reset usage for org {OrgId}, metric {Metric}", organizationId, metricType);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error resetting usage for org {OrgId}", organizationId);
            }
        }

        /// <inheritdoc/>
        public async Task<QuotaLimits> GetEffectiveLimitsAsync(Guid organizationId)
        {
            try
            {
                var orgLimits = await this._tenantService.GetOrganizationLimitsAsync(organizationId).ConfigureAwait(false);

                return new QuotaLimits
                {
                    MaxConcurrentRequests = orgLimits.MaxConcurrentRequests,
                    MonthlyRequestLimit = orgLimits.MonthlyRequestLimit,
                    MonthlyTokenLimit = orgLimits.MonthlyTokenLimit,
                    RequestsPerMinute = this.DeriveRpmLimit(orgLimits.MonthlyRequestLimit),
                    TokensPerMinute = this.DeriveTpmLimit(orgLimits.MonthlyTokenLimit),
                };
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error getting limits for org {OrgId}", organizationId);

                // Return default free tier limits
                return new QuotaLimits
                {
                    MaxConcurrentRequests = 10,
                    MonthlyRequestLimit = 10000,
                    MonthlyTokenLimit = 100000,
                    RequestsPerMinute = 100,
                    TokensPerMinute = 1000,
                };
            }
        }

        private async Task<QuotaResult> CheckConcurrentLimitAsync(Guid organizationId, QuotaLimits limits)
        {
            try
            {
                var db = this._redis.GetDatabase();
                var key = $"quota:org:{organizationId}:concurrent";
                var current = await db.StringGetAsync(key).ConfigureAwait(false);
                var currentCount = current.HasValue ? (long)current : 0;

                if (currentCount >= limits.MaxConcurrentRequests)
                {
                    this._logger.LogWarning(
                        "Concurrent request limit exceeded for org {OrgId}: {Current}/{Limit}",
                        organizationId, currentCount, limits.MaxConcurrentRequests);

                    return QuotaResult.Blocked($"Concurrent request limit exceeded: {currentCount}/{limits.MaxConcurrentRequests}");
                }

                return QuotaResult.Allowed();
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error checking concurrent limit for org {OrgId}", organizationId);
                return QuotaResult.Allowed();
            }
        }

        private string BuildQuotaKey(Guid organizationId, Guid? userId, Guid? virtualKeyId, string metricType, string granularity)
        {
            var keyParts = new List<string> { "quota", "org", organizationId.ToString() };

            if (userId.HasValue)
            {
                keyParts.AddRange(new[] { "user", userId.Value.ToString() });
            }

            if (virtualKeyId.HasValue)
            {
                keyParts.AddRange(new[] { "key", virtualKeyId.Value.ToString() });
            }

            keyParts.Add(metricType);
            keyParts.Add(granularity);

            return string.Join(":", keyParts);
        }

        private long DetermineLimit(QuotaCheckRequest request, QuotaLimits limits)
        {
            return request.TimeGranularity switch
            {
                "minute" when string.Equals(request.MetricType, "requests", StringComparison.Ordinal) => limits.RequestsPerMinute,
                "minute" when string.Equals(request.MetricType, "tokens", StringComparison.Ordinal) => limits.TokensPerMinute,
                "month" when string.Equals(request.MetricType, "requests", StringComparison.Ordinal) => limits.MonthlyRequestLimit,
                "month" when string.Equals(request.MetricType, "tokens", StringComparison.Ordinal) => limits.MonthlyTokenLimit,
                _ => 0, // No limit for other granularities
            };
        }

        private int GetWindowSeconds(string granularity)
        {
            return granularity switch
            {
                "minute" => 60,
                "hour" => 3600,
                "day" => 86400,
                "week" => 604800,
                "month" => 2592000, // 30 days
                _ => 60,
            };
        }

        private DateTime CalculateWindowStart(string granularity, WindowType windowType)
        {
            var now = DateTime.UtcNow;

            if (windowType == WindowType.Sliding)
            {
                return now.AddSeconds(-this.GetWindowSeconds(granularity));
            }

            // Fixed window
            return granularity switch
            {
                "minute" => new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, DateTimeKind.Utc),
                "hour" => new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc),
                "day" => new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc),
                "week" => now.AddDays(-(int)now.DayOfWeek).Date,
                "month" => new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                _ => now,
            };
        }

        private int DeriveRpmLimit(long monthlyLimit)
        {
            // Assume ~30 days, 24 hours, 60 minutes
            // But add 20% buffer for burst traffic
            var avgPerMinute = monthlyLimit / (30.0 * 24 * 60);
            return Math.Max(1, (int)(avgPerMinute * 1.2));
        }

        private int DeriveTpmLimit(long monthlyLimit)
        {
            var avgPerMinute = monthlyLimit / (30.0 * 24 * 60);
            return Math.Max(1, (int)(avgPerMinute * 1.2));
        }
    }
}
