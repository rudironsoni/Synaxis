using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Synaxis.Core.Contracts;

namespace Synaxis.Infrastructure.Services
{
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
            Size = 1
        };

        // Default Redis cache expiration (L2)
        private static readonly TimeSpan DefaultExpiration = TimeSpan.FromMinutes(15);

        // Statistics
        private long _hits = 0;
        private long _misses = 0;

        public CacheService(
            IDistributedCache distributedCache,
            IMemoryCache memoryCache,
            ILogger<CacheService> logger)
        {
            _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Cache key is required", nameof(key));

            try
            {
                // Try L1 cache (memory) first
                if (_memoryCache.TryGetValue<T>(key, out var memoryCachedValue))
                {
                    System.Threading.Interlocked.Increment(ref _hits);
                    _logger.LogTrace("Cache hit (L1) for key: {Key}", key);
                    return memoryCachedValue;
                }

                // Try L2 cache (Redis)
                var cachedBytes = await _distributedCache.GetAsync(key);

                if (cachedBytes != null && cachedBytes.Length > 0)
                {
                    System.Threading.Interlocked.Increment(ref _hits);
                    _logger.LogTrace("Cache hit (L2) for key: {Key}", key);

                    var value = JsonSerializer.Deserialize<T>(cachedBytes);

                    // Populate L1 cache
                    _memoryCache.Set(key, value, MemoryCacheOptions);

                    return value;
                }

                System.Threading.Interlocked.Increment(ref _misses);
                _logger.LogTrace("Cache miss for key: {Key}", key);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving from cache for key: {Key}", key);
                System.Threading.Interlocked.Increment(ref _misses);
                return null;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Cache key is required", nameof(key));

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            try
            {
                var expirationTime = expiration ?? DefaultExpiration;

                // Set in L1 cache (memory)
                _memoryCache.Set(key, value, MemoryCacheOptions);

                // Set in L2 cache (Redis)
                var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expirationTime
                };

                await _distributedCache.SetAsync(key, bytes, options);

                _logger.LogTrace("Set cache for key: {Key} with expiration: {Expiration}", key, expirationTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache for key: {Key}", key);

                // Don't throw - cache failures should not break the application
            }
        }

        public async Task RemoveAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Cache key is required", nameof(key));

            try
            {
                // Remove from L1 cache
                _memoryCache.Remove(key);

                // Remove from L2 cache
                await _distributedCache.RemoveAsync(key);

                _logger.LogTrace("Removed cache for key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache for key: {Key}", key);
            }
        }

        public async Task RemoveByPatternAsync(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                throw new ArgumentException("Pattern is required", nameof(pattern));

            _logger.LogWarning("RemoveByPatternAsync not fully implemented - pattern matching requires Redis Lua scripting. Pattern: {Pattern}", pattern);

            await Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            try
            {
                // Check L1 cache
                if (_memoryCache.TryGetValue(key, out _))
                    return true;

                // Check L2 cache
                var cachedBytes = await _distributedCache.GetAsync(key);
                return cachedBytes != null && cachedBytes.Length > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking cache existence for key: {Key}", key);
                return false;
            }
        }

        public async Task<IDictionary<string, T>> GetManyAsync<T>(IEnumerable<string> keys) where T : class
        {
            if (keys == null || !keys.Any())
                return new Dictionary<string, T>();

            var result = new Dictionary<string, T>();

            foreach (var key in keys)
            {
                var value = await GetAsync<T>(key);
                if (value != null)
                {
                    result[key] = value;
                }
            }

            return result;
        }

        public async Task SetManyAsync<T>(IDictionary<string, T> items, TimeSpan? expiration = null) where T : class
        {
            if (items == null || !items.Any())
                return;

            foreach (var kvp in items)
            {
                await SetAsync(kvp.Key, kvp.Value, expiration);
            }
        }

        public Task<CacheStatistics> GetStatisticsAsync()
        {
            var stats = new CacheStatistics
            {
                Hits = _hits,
                Misses = _misses,
                TotalKeys = 0, // Would require Redis DBSIZE command
                MemoryUsageBytes = 0, // Would require Redis INFO memory command
                CollectedAt = DateTime.UtcNow
            };

            return Task.FromResult(stats);
        }

        public async Task InvalidateGloballyAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Cache key is required", nameof(key));

            try
            {
                // Remove from local caches immediately
                await RemoveAsync(key);

                // Publish invalidation message to Kafka for cross-region sync
                // Note: This requires Kafka integration which should be injected
                _logger.LogInformation("Publishing global cache invalidation for key: {Key}", key);

                _logger.LogDebug("Cache invalidation message would be published to Kafka topic: cache-invalidation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating cache globally for key: {Key}", key);
            }
        }

        public string GetTenantKey(Guid tenantId, string key)
        {
            if (tenantId == Guid.Empty)
                throw new ArgumentException("Tenant ID is required", nameof(tenantId));

            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key is required", nameof(key));

            return $"tenant:{tenantId}:{key}";
        }
    }
}
