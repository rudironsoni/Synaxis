// <copyright file="ICacheService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Multi-level cache service with eventual consistency support.
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Get value from cache.
        /// </summary>
        /// <typeparam name="T">The type of the cached value.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the cached value or null.</returns>
        Task<T?> GetAsync<T>(string key)
        where T : class;

        /// <summary>
        /// Set value in cache with expiration.
        /// </summary>
        /// <typeparam name="T">The type of the value to cache.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="value">The value to cache.</param>
        /// <param name="expiration">The optional expiration time span.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        where T : class;

        /// <summary>
        /// Remove value from cache.
        /// </summary>
        /// <param name="key">The cache key to remove.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task RemoveAsync(string key);

        /// <summary>
        /// Remove values matching pattern from cache.
        /// </summary>
        /// <param name="pattern">The pattern to match cache keys.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task RemoveByPatternAsync(string pattern);

        /// <summary>
        /// Check if key exists in cache.
        /// </summary>
        /// <param name="key">The cache key to check.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether the key exists.</returns>
        Task<bool> ExistsAsync(string key);

        /// <summary>
        /// Get multiple values from cache.
        /// </summary>
        /// <typeparam name="T">The type of the cached values.</typeparam>
        /// <param name="keys">The collection of cache keys to retrieve.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the dictionary of key-value pairs.</returns>
        Task<IDictionary<string, T>> GetManyAsync<T>(IEnumerable<string> keys)
        where T : class;

        /// <summary>
        /// Set multiple values in cache.
        /// </summary>
        /// <typeparam name="T">The type of the values to cache.</typeparam>
        /// <param name="items">The dictionary of key-value pairs to cache.</param>
        /// <param name="expiration">The optional expiration time span.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task SetManyAsync<T>(IDictionary<string, T> items, TimeSpan? expiration = null)
        where T : class;

        /// <summary>
        /// Get cache statistics (hits, misses, etc.).
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the cache statistics.</returns>
        Task<CacheStatistics> GetStatisticsAsync();

        /// <summary>
        /// Invalidate cache across all regions (eventual consistency).
        /// </summary>
        /// <param name="key">The cache key to invalidate globally.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task InvalidateGloballyAsync(string key);

        /// <summary>
        /// Get tenant-scoped cache key.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <param name="key">The base cache key.</param>
        /// <returns>The tenant-scoped cache key.</returns>
        string GetTenantKey(Guid tenantId, string key);
    }

    /// <summary>
    /// Cache performance statistics.
    /// </summary>
    public class CacheStatistics
    {
        /// <summary>
        /// Gets or sets the number of cache hits.
        /// </summary>
        public long Hits { get; set; }

        /// <summary>
        /// Gets or sets the number of cache misses.
        /// </summary>
        public long Misses { get; set; }

        /// <summary>
        /// Gets the cache hit ratio.
        /// </summary>
        public double HitRatio => this.Hits + this.Misses > 0 ? (double)this.Hits / (this.Hits + this.Misses) : 0;

        /// <summary>
        /// Gets or sets the total number of keys in cache.
        /// </summary>
        public long TotalKeys { get; set; }

        /// <summary>
        /// Gets or sets the memory usage in bytes.
        /// </summary>
        public long MemoryUsageBytes { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when statistics were collected.
        /// </summary>
        public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
    }
}
