// <copyright file="RedisRateLimitingService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Services
{
    using System.Text;
    using Microsoft.Extensions.Logging;
    using StackExchange.Redis;

    /// <summary>
    /// Redis-based rate limiting service with atomic Lua script operations.
    /// Supports hierarchical rate limiting (User → Group → Organization) for both RPM and TPM.
    /// </summary>
    public class RedisRateLimitingService
    {
        // Lua script for atomic rate limit check and increment
        // Returns: [current_count, ttl_seconds]
        private const string LuaCheckAndIncrement = @"
            local key = KEYS[1]
            local limit = tonumber(ARGV[1])
            local window = tonumber(ARGV[2])
            local increment = tonumber(ARGV[3])

            local current = redis.call('GET', key)

            if current == false then
                -- First request in window
                redis.call('SET', key, increment, 'EX', window)
                return {increment, window}
            end

            current = tonumber(current)

            if current >= limit then
                -- Rate limit exceeded
                local ttl = redis.call('TTL', key)
                return {current, ttl}
            end

            -- Increment and return
            local new_count = redis.call('INCRBY', key, increment)
            local ttl = redis.call('TTL', key)
            return {new_count, ttl}
        ";

        // Lua script for token usage increment only (for post-request tracking)
        // Returns: [new_count, ttl_seconds]
        private const string LuaIncrementTokens = @"
            local key = KEYS[1]
            local tokens = tonumber(ARGV[1])
            local window = tonumber(ARGV[2])

            local current = redis.call('GET', key)

            if current == false then
                redis.call('SET', key, tokens, 'EX', window)
                return {tokens, window}
            end

            local new_count = redis.call('INCRBY', key, tokens)
            local ttl = redis.call('TTL', key)
            return {new_count, ttl}
        ";

        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisRateLimitingService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisRateLimitingService"/> class.
        /// </summary>
        /// <param name="redis">The Redis connection multiplexer.</param>
        /// <param name="logger">The logger instance.</param>
        public RedisRateLimitingService(
            IConnectionMultiplexer redis,
            ILogger<RedisRateLimitingService> logger)
        {
            ArgumentNullException.ThrowIfNull(redis);
            this._redis = redis;
            ArgumentNullException.ThrowIfNull(logger);
            this._logger = logger;
        }

        /// <summary>
        /// Checks rate limit for a single key using atomic Lua script.
        /// </summary>
        /// <param name="key">The rate limit key.</param>
        /// <param name="limit">The maximum number of requests/tokens allowed.</param>
        /// <param name="window">The time window for rate limiting.</param>
        /// <param name="increment">The amount to increment (default is 1 for request counting).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="RateLimitResult"/> indicating whether the request is allowed.</returns>
        public async Task<RateLimitResult> CheckRateLimitAsync(
            string key,
            int limit,
            TimeSpan window,
            int increment = 1,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            }

            if (limit <= 0)
            {
                throw new ArgumentException("Limit must be greater than zero.", nameof(limit));
            }

            try
            {
                var db = this._redis.GetDatabase();
                var windowSeconds = (int)window.TotalSeconds;

                // Execute Lua script atomically
                var result = await db.ScriptEvaluateAsync(
                    LuaCheckAndIncrement,
                    new RedisKey[] { key },
                    new RedisValue[] { limit, windowSeconds, increment }).ConfigureAwait(false);

                var values = (RedisValue[])result!;
                var current = (long)values[0];
                var ttl = (long)values[1];

                if (current > limit)
                {
                    this._logger.LogWarning(
                        "Rate limit exceeded for key {Key}: {Current}/{Limit}",
                        key,
                        current,
                        limit);

                    return RateLimitResult.Denied(current, limit, ttl, key);
                }

                this._logger.LogDebug(
                    "Rate limit check passed for key {Key}: {Current}/{Limit}",
                    key,
                    current,
                    limit);

                return RateLimitResult.Allowed(current, limit, ttl);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error checking rate limit for key {Key}", key);

                // Fail open: allow the request if Redis is unavailable
                return RateLimitResult.Allowed(0, limit, (int)window.TotalSeconds);
            }
        }

        /// <summary>
        /// Checks hierarchical rate limits: User → Group → Organization.
        /// Each level is checked independently, and the most restrictive limit applies.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="groupId">The optional group ID.</param>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="config">The rate limit configuration with limits for each level.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="RateLimitResult"/> from the most restrictive level that denies the request, or allowed if all pass.</returns>
        public async Task<RateLimitResult> CheckHierarchicalRateLimitAsync(
            Guid userId,
            Guid? groupId,
            Guid organizationId,
            RateLimitConfig config,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(config);
            if (!config.HasLimits())
            {
                this._logger.LogDebug("No rate limits configured, allowing request");
                return RateLimitResult.Allowed(0, 0, 0);
            }

            // Build and execute all rate limit checks
            var checks = this.BuildRateLimitChecks(userId, groupId, organizationId, config, cancellationToken);
            var results = await Task.WhenAll(checks).ConfigureAwait(false);

            // Find first denied result or return most restrictive allowed
            return this.ProcessRateLimitResults(results);
        }

        /// <summary>
        /// Increments token usage after a request completes.
        /// This is separate from the pre-request check to handle actual token counts.
        /// </summary>
        /// <param name="key">The rate limit key (should match the key used in CheckRateLimitAsync).</param>
        /// <param name="tokenCount">The number of tokens to add.</param>
        /// <param name="window">The time window for rate limiting.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The updated token count.</returns>
        public async Task<long> IncrementTokenUsageAsync(
            string key,
            int tokenCount,
            TimeSpan window,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            }

            if (tokenCount < 0)
            {
                throw new ArgumentException("Token count cannot be negative.", nameof(tokenCount));
            }

            try
            {
                var db = this._redis.GetDatabase();
                var windowSeconds = (int)window.TotalSeconds;

                // Execute Lua script atomically
                var result = await db.ScriptEvaluateAsync(
                    LuaIncrementTokens,
                    new RedisKey[] { key },
                    new RedisValue[] { tokenCount, windowSeconds }).ConfigureAwait(false);

                var values = (RedisValue[])result!;
                var newCount = (long)values[0];

                this._logger.LogDebug(
                    "Incremented token usage for key {Key}: +{Tokens} = {NewCount}",
                    key,
                    tokenCount,
                    newCount);

                return newCount;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error incrementing token usage for key {Key}", key);
                return 0;
            }
        }

        /// <summary>
        /// Increments token usage for all hierarchical levels after a request completes.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="groupId">The optional group ID.</param>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="tokenCount">The number of tokens to add.</param>
        /// <param name="window">The time window for rate limiting.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task IncrementHierarchicalTokenUsageAsync(
            Guid userId,
            Guid? groupId,
            Guid organizationId,
            int tokenCount,
            TimeSpan window,
            CancellationToken cancellationToken = default)
        {
            var tasks = new List<Task<long>>
            {
                this.IncrementTokenUsageAsync(BuildKey("user", userId.ToString(), "tpm"), tokenCount, window, cancellationToken),
                this.IncrementTokenUsageAsync(BuildKey("org", organizationId.ToString(), "tpm"), tokenCount, window, cancellationToken),
            };

            if (groupId.HasValue)
            {
                tasks.Add(this.IncrementTokenUsageAsync(BuildKey("group", groupId.Value.ToString(), "tpm"), tokenCount, window, cancellationToken));
            }

            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Gets current usage for a specific rate limit key.
        /// </summary>
        /// <param name="key">The rate limit key.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The current usage count, or 0 if not found.</returns>
        public async Task<long> GetCurrentUsageAsync(
            string key,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var db = this._redis.GetDatabase();
                var value = await db.StringGetAsync(key).ConfigureAwait(false);
                return value.HasValue ? (long)value : 0;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error getting current usage for key {Key}", key);
                return 0;
            }
        }

        /// <summary>
        /// Resets rate limit for a specific key (useful for testing or manual overrides).
        /// </summary>
        /// <param name="key">The rate limit key to reset.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the key was deleted, false otherwise.</returns>
        public async Task<bool> ResetRateLimitAsync(
            string key,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var db = this._redis.GetDatabase();
                var deleted = await db.KeyDeleteAsync(key).ConfigureAwait(false);

                if (deleted)
                {
                    this._logger.LogInformation("Reset rate limit for key {Key}", key);
                }

                return deleted;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error resetting rate limit for key {Key}", key);
                return false;
            }
        }

        /// <summary>
        /// Builds a Redis key for rate limiting with proper namespacing.
        /// </summary>
        /// <param name="scope">The scope (user, group, org).</param>
        /// <param name="id">The identifier.</param>
        /// <param name="type">The type (rpm or tpm).</param>
        /// <returns>A properly formatted Redis key.</returns>
        private static string BuildKey(string scope, string id, string type)
        {
            return $"ratelimit:{scope}:{id}:{type}";
        }

        /// <summary>
        /// Helper method to check rate limit and attach metadata.
        /// </summary>
        private async Task<(RateLimitResult Result, string Scope, string Type)> CheckWithMetadataAsync(
            string key,
            int limit,
            TimeSpan window,
            string scope,
            string type,
            CancellationToken cancellationToken,
            int increment = 1)
        {
            var result = await this.CheckRateLimitAsync(key, limit, window, increment, cancellationToken).ConfigureAwait(false);
            return (result, scope, type);
        }

        private List<Task<(RateLimitResult Result, string Scope, string Type)>> BuildRateLimitChecks(
            Guid userId,
            Guid? groupId,
            Guid organizationId,
            RateLimitConfig config,
            CancellationToken cancellationToken)
        {
            var window = config.Window;
            var checks = new List<Task<(RateLimitResult Result, string Scope, string Type)>>();

            // User-level checks
            this.AddUserLevelChecks(checks, userId, config, window, cancellationToken);

            // Group-level checks
            if (groupId.HasValue)
            {
                this.AddGroupLevelChecks(checks, groupId.Value, config, window, cancellationToken);
            }

            // Organization-level checks
            this.AddOrganizationLevelChecks(checks, organizationId, config, window, cancellationToken);

            return checks;
        }

        private void AddUserLevelChecks(
            List<Task<(RateLimitResult Result, string Scope, string Type)>> checks,
            Guid userId,
            RateLimitConfig config,
            TimeSpan window,
            CancellationToken cancellationToken)
        {
            if (config.UserRpm.HasValue)
            {
                var key = BuildKey("user", userId.ToString(), "rpm");
                checks.Add(this.CheckWithMetadataAsync(key, config.UserRpm.Value, window, "User", "RPM", cancellationToken));
            }

            if (config.UserTpm.HasValue)
            {
                var key = BuildKey("user", userId.ToString(), "tpm");
                checks.Add(this.CheckWithMetadataAsync(key, config.UserTpm.Value, window, "User", "TPM", cancellationToken, increment: 0));
            }
        }

        private void AddGroupLevelChecks(
            List<Task<(RateLimitResult Result, string Scope, string Type)>> checks,
            Guid groupId,
            RateLimitConfig config,
            TimeSpan window,
            CancellationToken cancellationToken)
        {
            if (config.GroupRpm.HasValue)
            {
                var key = BuildKey("group", groupId.ToString(), "rpm");
                checks.Add(this.CheckWithMetadataAsync(key, config.GroupRpm.Value, window, "Group", "RPM", cancellationToken));
            }

            if (config.GroupTpm.HasValue)
            {
                var key = BuildKey("group", groupId.ToString(), "tpm");
                checks.Add(this.CheckWithMetadataAsync(key, config.GroupTpm.Value, window, "Group", "TPM", cancellationToken, increment: 0));
            }
        }

        private void AddOrganizationLevelChecks(
            List<Task<(RateLimitResult Result, string Scope, string Type)>> checks,
            Guid organizationId,
            RateLimitConfig config,
            TimeSpan window,
            CancellationToken cancellationToken)
        {
            if (config.OrganizationRpm.HasValue)
            {
                var key = BuildKey("org", organizationId.ToString(), "rpm");
                checks.Add(this.CheckWithMetadataAsync(key, config.OrganizationRpm.Value, window, "Organization", "RPM", cancellationToken));
            }

            if (config.OrganizationTpm.HasValue)
            {
                var key = BuildKey("org", organizationId.ToString(), "tpm");
                checks.Add(this.CheckWithMetadataAsync(key, config.OrganizationTpm.Value, window, "Organization", "TPM", cancellationToken, increment: 0));
            }
        }

        private RateLimitResult ProcessRateLimitResults((RateLimitResult Result, string Scope, string Type)[] results)
        {
            // Find the first denied result (most restrictive)
            var denied = results.FirstOrDefault(r => !r.Result.IsAllowed);
            if (denied != default)
            {
                this._logger.LogWarning(
                    "Hierarchical rate limit denied at {Scope} level ({Type}): {Current}/{Limit}",
                    denied.Scope,
                    denied.Type,
                    denied.Result.Current,
                    denied.Result.Limit);

                denied.Result.LimitedBy = denied.Scope;
                denied.Result.LimitType = denied.Type;
                return denied.Result;
            }

            // All checks passed - return the most restrictive allowed result
            var mostRestrictive = results.OrderBy(r => r.Result.Remaining).First();

            mostRestrictive.Result.LimitedBy = mostRestrictive.Scope;
            mostRestrictive.Result.LimitType = mostRestrictive.Type;

            this._logger.LogDebug(
                "Hierarchical rate limit check passed. Most restrictive: {Scope} {Type} with {Remaining} remaining",
                mostRestrictive.Scope,
                mostRestrictive.Type,
                mostRestrictive.Result.Remaining);

            return mostRestrictive.Result;
        }
    }
}
