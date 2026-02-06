using System.Threading.Tasks;
using Synaxis.Core.Models;

namespace Synaxis.Core.Contracts
{
    /// <summary>
    /// Routes requests to appropriate region based on user data residency
    /// </summary>
    public interface IRegionRouter
    {
        /// <summary>
        /// Get the region where a user's data is stored
        /// </summary>
        Task<string> GetUserRegionAsync(System.Guid userId);
        
        /// <summary>
        /// Route request to user's region
        /// </summary>
        Task<TResponse> RouteToUserRegionAsync<TRequest, TResponse>(
            System.Guid userId, 
            string endpoint, 
            TRequest request);
        
        /// <summary>
        /// Check if routing would be cross-border
        /// </summary>
        Task<bool> IsCrossBorderAsync(string fromRegion, string toRegion);
        
        /// <summary>
        /// Process request locally (current region)
        /// </summary>
        Task<TResponse> ProcessLocallyAsync<TRequest, TResponse>(TRequest request);
        
        /// <summary>
        /// Check if cross-border consent is required
        /// </summary>
        Task<bool> RequiresCrossBorderConsentAsync(System.Guid userId, string targetRegion);
        
        /// <summary>
        /// Get nearest healthy region for failover
        /// </summary>
        Task<string> GetNearestHealthyRegionAsync(string currentRegion, GeoLocation userLocation);
        
        /// <summary>
        /// Log cross-border transfer for compliance
        /// </summary>
        Task LogCrossBorderTransferAsync(CrossBorderTransferContext context);
    }
    
    public class CrossBorderTransferContext
    {
        public System.Guid OrganizationId { get; set; }
        public System.Guid? UserId { get; set; }
        public string FromRegion { get; set; }
        public string ToRegion { get; set; }
        public string LegalBasis { get; set; } // 'SCC', 'consent', 'adequacy'
        public string Purpose { get; set; }
        public string[] DataCategories { get; set; }
    }
    
    public class GeoLocation
    {
        public string IpAddress { get; set; }
        public string CountryCode { get; set; }
        public string CountryName { get; set; }
        public string City { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string ContinentCode { get; set; }
        public string TimeZone { get; set; }
    }
}
