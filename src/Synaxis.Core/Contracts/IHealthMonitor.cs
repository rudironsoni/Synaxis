using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Synaxis.Core.Contracts
{
    /// <summary>
    /// Monitors health of regional infrastructure and external providers
    /// </summary>
    public interface IHealthMonitor
    {
        /// <summary>
        /// Checks health of a specific region
        /// </summary>
        Task<RegionHealth> CheckRegionHealthAsync(string region);
        
        /// <summary>
        /// Gets health status for all regions
        /// </summary>
        Task<Dictionary<string, RegionHealth>> GetAllRegionHealthAsync();
        
        /// <summary>
        /// Checks database connectivity for a region
        /// </summary>
        Task<bool> CheckDatabaseHealthAsync(string region);
        
        /// <summary>
        /// Checks Redis connectivity for a region
        /// </summary>
        Task<bool> CheckRedisHealthAsync(string region);
        
        /// <summary>
        /// Checks external provider health (OpenAI, Anthropic, etc.)
        /// </summary>
        Task<ProviderHealth> CheckProviderHealthAsync(string provider);
        
        /// <summary>
        /// Gets aggregate health score for a region (0-100)
        /// </summary>
        Task<int> GetHealthScoreAsync(string region);
        
        /// <summary>
        /// Determines if a region is healthy enough to accept traffic
        /// </summary>
        Task<bool> IsRegionHealthyAsync(string region);
        
        /// <summary>
        /// Gets the nearest healthy region to a given region
        /// </summary>
        Task<string> GetNearestHealthyRegionAsync(string fromRegion, List<string> availableRegions);
    }
    
    public class RegionHealth
    {
        public string Region { get; set; }
        public bool IsHealthy { get; set; }
        public int HealthScore { get; set; } // 0-100
        public bool DatabaseHealthy { get; set; }
        public bool RedisHealthy { get; set; }
        public Dictionary<string, bool> ProviderHealth { get; set; } = new Dictionary<string, bool>();
        public DateTime LastChecked { get; set; }
        public string Status { get; set; } // "healthy", "degraded", "unhealthy"
        public List<string> Issues { get; set; } = new List<string>();
        
        // Additional metrics for SuperAdmin dashboard
        public double ResponseTimeMs { get; set; }
        public double ErrorRate { get; set; }
        public int ActiveConnections { get; set; }
        public string Version { get; set; }
    }
    
    public class ProviderHealth
    {
        public string Provider { get; set; }
        public bool IsAvailable { get; set; }
        public int ResponseTimeMs { get; set; }
        public string Status { get; set; }
        public DateTime LastChecked { get; set; }
    }
}
