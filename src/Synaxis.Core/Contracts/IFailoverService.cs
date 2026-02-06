using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Synaxis.Core.Contracts
{
    /// <summary>
    /// Manages automatic failover between regions based on health status
    /// </summary>
    public interface IFailoverService
    {
        /// <summary>
        /// Selects the best available region for a request
        /// </summary>
        Task<FailoverDecision> SelectRegionAsync(Guid organizationId, Guid userId, string primaryRegion);
        
        /// <summary>
        /// Handles failover when primary region is unhealthy
        /// </summary>
        Task<FailoverResult> FailoverAsync(Guid organizationId, Guid userId, string fromRegion, string toRegion);
        
        /// <summary>
        /// Checks if user has given cross-border consent
        /// </summary>
        Task<bool> HasCrossBorderConsentAsync(Guid userId);
        
        /// <summary>
        /// Records cross-border transfer for compliance
        /// </summary>
        Task RecordCrossBorderTransferAsync(Guid organizationId, Guid userId, string fromRegion, string toRegion, string legalBasis);
        
        /// <summary>
        /// Checks if primary region has recovered and can handle requests again
        /// </summary>
        Task<bool> CanRecoverToPrimaryAsync(string region);
        
        /// <summary>
        /// Gets failover notification message for user
        /// </summary>
        string GetFailoverNotificationMessage(string fromRegion, string toRegion, bool needsConsent);
    }
    
    public class FailoverDecision
    {
        public string SelectedRegion { get; set; }
        public bool IsFailover { get; set; }
        public bool NeedsCrossBorderConsent { get; set; }
        public string Reason { get; set; }
        public List<string> HealthyRegions { get; set; } = new List<string>();
    }
    
    public class FailoverResult
    {
        public bool Success { get; set; }
        public string TargetRegion { get; set; }
        public string Message { get; set; }
        public bool ConsentRequired { get; set; }
        public string ConsentUrl { get; set; }
        
        public static FailoverResult Succeeded(string targetRegion, string message) 
            => new() { Success = true, TargetRegion = targetRegion, Message = message };
            
        public static FailoverResult Failed(string message) 
            => new() { Success = false, Message = message };
            
        public static FailoverResult NeedsConsent(string targetRegion, string consentUrl) 
            => new() { Success = false, TargetRegion = targetRegion, ConsentRequired = true, ConsentUrl = consentUrl };
    }
}
