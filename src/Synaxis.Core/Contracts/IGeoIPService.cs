using System.Threading.Tasks;

namespace Synaxis.Core.Contracts
{
    /// <summary>
    /// Service for IP to location lookup
    /// </summary>
    public interface IGeoIPService
    {
        /// <summary>
        /// Get location from IP address
        /// </summary>
        Task<GeoLocation> GetLocationAsync(string ipAddress);
        
        /// <summary>
        /// Get region based on country code
        /// </summary>
        Task<string> GetRegionForCountryAsync(string countryCode);
        
        /// <summary>
        /// Get region based on IP address
        /// </summary>
        Task<string> GetRegionForIpAsync(string ipAddress);
        
        /// <summary>
        /// Check if IP is in EU region
        /// </summary>
        Task<bool> IsEuRegionAsync(string ipAddress);
        
        /// <summary>
        /// Check if country requires data residency
        /// </summary>
        Task<bool> RequiresDataResidencyAsync(string countryCode);
    }
}
