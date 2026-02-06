using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Synaxis.Core.Contracts
{
    /// <summary>
    /// Multi-level cache service with eventual consistency support
    /// </summary>
    public interface ICacheService
    {
        /// <summary>
        /// Get value from cache
        /// </summary>
        Task<T> GetAsync<T>(string key) where T : class;
        
        /// <summary>
        /// Set value in cache with expiration
        /// </summary>
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;
        
        /// <summary>
        /// Remove value from cache
        /// </summary>
        Task RemoveAsync(string key);
        
        /// <summary>
        /// Remove values matching pattern from cache
        /// </summary>
        Task RemoveByPatternAsync(string pattern);
        
        /// <summary>
        /// Check if key exists in cache
        /// </summary>
        Task<bool> ExistsAsync(string key);
        
        /// <summary>
        /// Get multiple values from cache
        /// </summary>
        Task<Dictionary<string, T>> GetManyAsync<T>(IEnumerable<string> keys) where T : class;
        
        /// <summary>
        /// Set multiple values in cache
        /// </summary>
        Task SetManyAsync<T>(Dictionary<string, T> items, TimeSpan? expiration = null) where T : class;
        
        /// <summary>
        /// Get cache statistics (hits, misses, etc.)
        /// </summary>
        Task<CacheStatistics> GetStatisticsAsync();
        
        /// <summary>
        /// Invalidate cache across all regions (eventual consistency)
        /// </summary>
        Task InvalidateGloballyAsync(string key);
        
        /// <summary>
        /// Get tenant-scoped cache key
        /// </summary>
        string GetTenantKey(Guid tenantId, string key);
    }
    
    /// <summary>
    /// Cache performance statistics
    /// </summary>
    public class CacheStatistics
    {
        public long Hits { get; set; }
        public long Misses { get; set; }
        public double HitRatio => Hits + Misses > 0 ? (double)Hits / (Hits + Misses) : 0;
        public long TotalKeys { get; set; }
        public long MemoryUsageBytes { get; set; }
        public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
    }
}
