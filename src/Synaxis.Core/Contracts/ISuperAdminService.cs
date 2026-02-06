using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Synaxis.Core.Models;

namespace Synaxis.Core.Contracts
{
    /// <summary>
    /// Super Admin service for cross-region visibility and management with strict access controls
    /// </summary>
    public interface ISuperAdminService
    {
        /// <summary>
        /// Get all organizations across all regions
        /// </summary>
        Task<List<OrganizationSummary>> GetCrossRegionOrganizationsAsync();
        
        /// <summary>
        /// Generate impersonation token for tenant (requires approval)
        /// </summary>
        Task<ImpersonationToken> GenerateImpersonationTokenAsync(ImpersonationRequest request);
        
        /// <summary>
        /// Get global usage analytics across all regions
        /// </summary>
        Task<GlobalUsageAnalytics> GetGlobalUsageAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null);
        
        /// <summary>
        /// Get cross-border transfer reports
        /// </summary>
        Task<List<CrossBorderTransferReport>> GetCrossBorderTransfersAsync(DateTime? startDate = null, DateTime? endDate = null);
        
        /// <summary>
        /// Get compliance status dashboard across regions
        /// </summary>
        Task<ComplianceStatusDashboard> GetComplianceStatusAsync();
        
        /// <summary>
        /// Get system health overview across all regions
        /// </summary>
        Task<SystemHealthOverview> GetSystemHealthOverviewAsync();
        
        /// <summary>
        /// Modify organization limits (requires approval)
        /// </summary>
        Task<bool> ModifyOrganizationLimitsAsync(LimitModificationRequest request);
        
        /// <summary>
        /// Verify super admin access (MFA, IP allowlist, business hours)
        /// </summary>
        Task<SuperAdminAccessValidation> ValidateAccessAsync(SuperAdminAccessContext context);
    }
    
    public class OrganizationSummary
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string PrimaryRegion { get; set; }
        public string Tier { get; set; }
        public int UserCount { get; set; }
        public int TeamCount { get; set; }
        public long MonthlyRequests { get; set; }
        public decimal MonthlySpend { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    
    public class ImpersonationRequest
    {
        public Guid UserId { get; set; }
        public Guid OrganizationId { get; set; }
        public string Justification { get; set; }
        public string ApprovedBy { get; set; }
        public int DurationMinutes { get; set; } = 15;
    }
    
    public class ImpersonationToken
    {
        public string Token { get; set; }
        public Guid UserId { get; set; }
        public Guid OrganizationId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string Justification { get; set; }
    }
    
    public class GlobalUsageAnalytics
    {
        public long TotalRequests { get; set; }
        public long TotalTokens { get; set; }
        public decimal TotalSpend { get; set; }
        public int TotalOrganizations { get; set; }
        public int TotalUsers { get; set; }
        public int ActiveOrganizations { get; set; }
        public Dictionary<string, RegionUsage> UsageByRegion { get; set; }
        public Dictionary<string, long> RequestsByModel { get; set; }
        public Dictionary<string, long> RequestsByProvider { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
    
    public class RegionUsage
    {
        public string Region { get; set; }
        public long Requests { get; set; }
        public long Tokens { get; set; }
        public decimal Spend { get; set; }
        public int Organizations { get; set; }
        public int Users { get; set; }
    }
    
    public class CrossBorderTransferReport
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public string OrganizationName { get; set; }
        public Guid? UserId { get; set; }
        public string UserEmail { get; set; }
        public string FromRegion { get; set; }
        public string ToRegion { get; set; }
        public string LegalBasis { get; set; }
        public string Purpose { get; set; }
        public string[] DataCategories { get; set; }
        public DateTime Timestamp { get; set; }
    }
    
    public class ComplianceStatusDashboard
    {
        public int TotalOrganizations { get; set; }
        public int CompliantOrganizations { get; set; }
        public int OrganizationsWithIssues { get; set; }
        public Dictionary<string, RegionCompliance> ComplianceByRegion { get; set; }
        public List<ComplianceIssue> Issues { get; set; }
        public DateTime CheckedAt { get; set; }
    }
    
    public class RegionCompliance
    {
        public string Region { get; set; }
        public bool IsCompliant { get; set; }
        public int TotalOrganizations { get; set; }
        public int OrganizationsWithConsent { get; set; }
        public int CrossBorderTransfers { get; set; }
        public List<string> Issues { get; set; }
    }
    
    public class ComplianceIssue
    {
        public string Severity { get; set; } // Critical, High, Medium, Low
        public string Category { get; set; }
        public string Description { get; set; }
        public Guid? OrganizationId { get; set; }
        public string OrganizationName { get; set; }
        public string Region { get; set; }
        public DateTime DetectedAt { get; set; }
    }
    
    public class SystemHealthOverview
    {
        public Dictionary<string, RegionHealth> HealthByRegion { get; set; }
        public bool AllRegionsHealthy { get; set; }
        public int TotalRegions { get; set; }
        public int HealthyRegions { get; set; }
        public List<SystemAlert> Alerts { get; set; }
        public DateTime CheckedAt { get; set; }
    }
    
    // RegionHealth is defined in IHealthMonitor.cs
    
    public class SystemAlert
    {
        public string Severity { get; set; } // Critical, Warning, Info
        public string Message { get; set; }
        public string Region { get; set; }
        public DateTime Timestamp { get; set; }
    }
    
    public class LimitModificationRequest
    {
        public Guid OrganizationId { get; set; }
        public string LimitType { get; set; } // MaxTeams, MaxUsersPerTeam, MaxKeysPerUser, etc.
        public int NewValue { get; set; }
        public string Justification { get; set; }
        public string ApprovedBy { get; set; }
    }
    
    public class SuperAdminAccessContext
    {
        public Guid UserId { get; set; }
        public string IpAddress { get; set; }
        public string MfaCode { get; set; }
        public string Action { get; set; }
        public string Justification { get; set; }
        public DateTime RequestTime { get; set; }
    }
    
    public class SuperAdminAccessValidation
    {
        public bool IsValid { get; set; }
        public string FailureReason { get; set; }
        public bool MfaRequired { get; set; }
        public bool MfaValid { get; set; }
        public bool IpAllowed { get; set; }
        public bool WithinBusinessHours { get; set; }
        public bool JustificationRequired { get; set; }
        public DateTime ValidatedAt { get; set; }
    }
}
