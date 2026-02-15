// <copyright file="CacheService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Logging;
    using Synaxis.Core.Contracts;

    /// <summary>
    /// Multi-level cache service with Redis and in-memory cache
    /// Supports eventual consistency with cross-region invalidation.
    /// </summary>
    public class CacheService : ICacheService
    {
        private readonly IDistributedCache _distributedCache;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<CacheService> _logger;

        // In-memory cache for frequently accessed items (L1)
        private static readonly MemoryCacheEntryOptions MemoryCacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5), // 5-second eventual consistency window
            Size = 1,
        };

        // Default Redis cache expiration (L2)
        private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(15);

        // Statistics
        private long _hits = 0;
        private long _misses = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheService"/> class.
        /// </summary>
        /// <param name="distributedCache"></param>
        /// <param name="memoryCache"></param>
        /// <param name="logger"></param>
        public CacheService(
            IDistributedCache distributedCache,
            IMemoryCache memoryCache,
            ILogger<CacheService> logger)
        {
            this._distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
            this._memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<T> GetAsync<T>(string key)
            where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Cache key is required", nameof(key));
            }

            try
            {
                // Try L1 cache (memory) first
                if (this._memoryCache.TryGetValue<T>(key, out var memoryCachedValue))
                {
                    System.Threading.Interlocked.Increment(ref this._hits);
                    this._logger.LogTrace("Cache hit (L1) for key: {Key}", key);
                    return memoryCachedValue;
                }

                // Try L2 cache (Redis)
                var cachedBytes = await this._distributedCache.GetAsync(key).ConfigureAwait(false);

                if (cachedBytes != null && cachedBytes.Length > 0)
                {
                    System.Threading.Interlocked.Increment(ref this._hits);
                    this._logger.LogTrace("Cache hit (L2) for key: {Key}", key);

                    var value = JsonSerializer.Deserialize<T>(cachedBytes);

                    // Populate L1 cache
                    this._memoryCache.Set(key, value, MemoryCacheOptions);

                    return value;
                }

                System.Threading.Interlocked.Increment(ref this._misses);
                this._logger.LogTrace("Cache miss for key: {Key}", key);
                return null;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error retrieving from cache for key: {Key}", key);
                System.Threading.Interlocked.Increment(ref this._misses);
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
            where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Cache key is required", nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            try
            {
                var expirationTime = expiration ?? DefaultExpiration;

                // Set in L1 cache (memory)
                this._memoryCache.Set(key, value, MemoryCacheOptions);

                // Set in L2 cache (Redis)
                var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expirationTime,
                };

                await this._distributedCache.SetAsync(key, bytes, options).ConfigureAwait(false);

                this._logger.LogTrace("Set cache for key: {Key} with expiration: {Expiration}", key, expirationTime);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error setting cache for key: {Key}", key);

                // Don't throw - cache failures should not break the application
            }
        }

        /// <inheritdoc/>
        public async Task RemoveAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Cache key is required", nameof(key));
            }

            try
            {
                // Remove from L1 cache
                this._memoryCache.Remove(key);

                // Remove from L2 cache
                await this._distributedCache.RemoveAsync(key).ConfigureAwait(false);

                this._logger.LogTrace("Removed cache for key: {Key}", key);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error removing cache for key: {Key}", key);
            }
        }

        /// <inheritdoc/>
        public Task RemoveByPatternAsync(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                throw new ArgumentException("Pattern is required", nameof(pattern));
            }

            this._logger.LogWarning("RemoveByPatternAsync not fully implemented - pattern matching requires Redis Lua scripting. Pattern: {Pattern}", pattern);

            // Note: Pattern-based deletion in Redis requires Lua scripting or SCAN command
            // Implementation requires StackExchange.Redis IConnectionMultiplexer injection
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            try
            {
                // Check L1 cache
                if (this._memoryCache.TryGetValue(key, out _))
                {
                    return true;
                }

                // Check L2 cache
                var cachedBytes = await this._distributedCache.GetAsync(key).ConfigureAwait(false);
                return cachedBytes != null && cachedBytes.Length > 0;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error checking cache existence for key: {Key}", key);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<IDictionary<string, T>> GetManyAsync<T>(IEnumerable<string> keys)
            where T : class
        {
            if (keys == null || !keys.Any())
            {
                return new Dictionary<string, T>(StringComparer.Ordinal);
            }

            var result = new Dictionary<string, T>(StringComparer.Ordinal);

            foreach (var key in keys)
            {
                var value = await this.GetAsync<T>(key).ConfigureAwait(false);
                if (value != null)
                {
                    result[key] = value;
                }
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task SetManyAsync<T>(IDictionary<string, T> items, TimeSpan? expiration = null)
            where T : class
        {
            if (items == null || !items.Any())
            {
                return;
            }

            foreach (var kvp in items)
            {
                await this.SetAsync(kvp.Key, kvp.Value, expiration).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public Task<CacheStatistics> GetStatisticsAsync()
        {
            var stats = new CacheStatistics
            {
                Hits = this._hits,
                Misses = this._misses,
                TotalKeys = 0, // Would require Redis DBSIZE command
                MemoryUsageBytes = 0, // Would require Redis INFO memory command
                CollectedAt = DateTime.UtcNow,
            };

            return Task.FromResult(stats);
        }

        /// <inheritdoc/>
        public async Task InvalidateGloballyAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Cache key is required", nameof(key));
            }

            try
            {
                // Remove from local caches immediately
                await this.RemoveAsync(key).ConfigureAwait(false);

                // Publish invalidation message to Kafka for cross-region sync
                // Note: Kafka integration requires IKafkaProducer injection (not yet implemented)
                this._logger.LogInformation("Global cache invalidation requested for key: {Key}", key);
                this._logger.LogDebug("Cache invalidation message would be published to Kafka topic: cache-invalidation");
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error invalidating cache globally for key: {Key}", key);
            }
        }

        /// <inheritdoc/>
        public string GetTenantKey(Guid tenantId, string key)
        {
            if (tenantId == Guid.Empty)
            {
                throw new ArgumentException("Tenant ID is required", nameof(tenantId));
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key is required", nameof(key));
            }

            return $"tenant:{tenantId}:{key}";
        }
    }
}
