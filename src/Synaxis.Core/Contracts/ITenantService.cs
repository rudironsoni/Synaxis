using System;
using System.Threading.Tasks;
using Synaxis.Core.Models;

namespace Synaxis.Core.Contracts
{
    /// <summary>
    /// Service for managing organizations (tenants)
    /// </summary>
    public interface ITenantService
    {
        /// <summary>
        /// Create a new organization
        /// </summary>
        Task<Organization> CreateOrganizationAsync(CreateOrganizationRequest request);
        
        /// <summary>
        /// Get organization by ID
        /// </summary>
        Task<Organization> GetOrganizationAsync(Guid id);
        
        /// <summary>
        /// Get organization by slug
        /// </summary>
        Task<Organization> GetOrganizationBySlugAsync(string slug);
        
        /// <summary>
        /// Update organization settings
        /// </summary>
        Task<Organization> UpdateOrganizationAsync(Guid id, UpdateOrganizationRequest request);
        
        /// <summary>
        /// Delete organization (soft delete)
        /// </summary>
        Task<bool> DeleteOrganizationAsync(Guid id);
        
        /// <summary>
        /// Get organization limits (merged from plan + overrides)
        /// </summary>
        Task<OrganizationLimits> GetOrganizationLimitsAsync(Guid organizationId);
        
        /// <summary>
        /// Check if organization can add more teams
        /// </summary>
        Task<bool> CanAddTeamAsync(Guid organizationId);
        
        /// <summary>
        /// Check if organization has reached concurrent request limit
        /// </summary>
        Task<bool> IsUnderConcurrentLimitAsync(Guid organizationId);
    }
    
    public class CreateOrganizationRequest
    {
        public string Name { get; set; }
        public string Slug { get; set; }
        public string PrimaryRegion { get; set; }
        public string BillingCurrency { get; set; } = "USD";
    }
    
    public class UpdateOrganizationRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Dictionary<string, object> Settings { get; set; }
    }
    
    public class OrganizationLimits
    {
        public int MaxTeams { get; set; }
        public int MaxUsersPerTeam { get; set; }
        public int MaxKeysPerUser { get; set; }
        public int MaxConcurrentRequests { get; set; }
        public long MonthlyRequestLimit { get; set; }
        public long MonthlyTokenLimit { get; set; }
        public int DataRetentionDays { get; set; }
    }
}
