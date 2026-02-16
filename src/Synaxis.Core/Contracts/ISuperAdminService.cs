// <copyright file="ISuperAdminService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Synaxis.Core.Models;

    /// <summary>
    /// Super Admin service for cross-region visibility and management with strict access controls.
    /// </summary>
    public interface ISuperAdminService
    {
        /// <summary>
        /// Get all organizations across all regions.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the list of organization summaries.</returns>
        Task<IList<OrganizationSummary>> GetCrossRegionOrganizationsAsync();

        /// <summary>
        /// Generate impersonation token for tenant (requires approval).
        /// </summary>
        /// <param name="request">The impersonation request details.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the impersonation token.</returns>
        Task<ImpersonationToken> GenerateImpersonationTokenAsync(ImpersonationRequest request);

        /// <summary>
        /// Get global usage analytics across all regions.
        /// </summary>
        /// <param name="startDate">The optional start date for analytics.</param>
        /// <param name="endDate">The optional end date for analytics.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the global usage analytics.</returns>
        Task<GlobalUsageAnalytics> GetGlobalUsageAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Get cross-border transfer reports.
        /// </summary>
        /// <param name="startDate">The optional start date for reports.</param>
        /// <param name="endDate">The optional end date for reports.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the list of transfer reports.</returns>
        Task<IList<CrossBorderTransferReport>> GetCrossBorderTransfersAsync(DateTime? startDate = null, DateTime? endDate = null);

        /// <summary>
        /// Get compliance status dashboard across regions.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the compliance status dashboard.</returns>
        Task<ComplianceStatusDashboard> GetComplianceStatusAsync();

        /// <summary>
        /// Get system health overview across all regions.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the system health overview.</returns>
        Task<SystemHealthOverview> GetSystemHealthOverviewAsync();

        /// <summary>
        /// Modify organization limits (requires approval).
        /// </summary>
        /// <param name="request">The limit modification request details.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether the modification was successful.</returns>
        Task<bool> ModifyOrganizationLimitsAsync(LimitModificationRequest request);

        /// <summary>
        /// Verify super admin access (MFA, IP allowlist, business hours).
        /// </summary>
        /// <param name="context">The access context to validate.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the access validation result.</returns>
        Task<SuperAdminAccessValidation> ValidateAccessAsync(SuperAdminAccessContext context);
    }

    /// <summary>
    /// Represents a summary of an organization.
    /// </summary>
    public class OrganizationSummary
    {
        /// <summary>
        /// Gets or sets the organization identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the organization name.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the organization slug.
        /// </summary>
        public required string Slug { get; set; }

        /// <summary>
        /// Gets or sets the primary region.
        /// </summary>
        public required string PrimaryRegion { get; set; }

        /// <summary>
        /// Gets or sets the tier.
        /// </summary>
        public required string Tier { get; set; }

        /// <summary>
        /// Gets or sets the user count.
        /// </summary>
        public int UserCount { get; set; }

        /// <summary>
        /// Gets or sets the team count.
        /// </summary>
        public int TeamCount { get; set; }

        /// <summary>
        /// Gets or sets the monthly request count.
        /// </summary>
        public long MonthlyRequests { get; set; }

        /// <summary>
        /// Gets or sets the monthly spend amount.
        /// </summary>
        public decimal MonthlySpend { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the organization is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Represents a request to impersonate a user.
    /// </summary>
    public class ImpersonationRequest
    {
        /// <summary>
        /// Gets or sets the user identifier to impersonate.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the organization identifier.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the justification for impersonation.
        /// </summary>
        public required string Justification { get; set; }

        /// <summary>
        /// Gets or sets the approver identifier.
        /// </summary>
        public required string ApprovedBy { get; set; }

        /// <summary>
        /// Gets or sets the duration in minutes.
        /// </summary>
        public int DurationMinutes { get; set; } = 15;
    }

    /// <summary>
    /// Represents an impersonation token.
    /// </summary>
    public class ImpersonationToken
    {
        /// <summary>
        /// Gets or sets the token value.
        /// </summary>
        public required string Token { get; set; }

        /// <summary>
        /// Gets or sets the user identifier being impersonated.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the organization identifier.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the expiration timestamp.
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets the justification for impersonation.
        /// </summary>
        public required string Justification { get; set; }
    }

    /// <summary>
    /// Represents global usage analytics across all regions.
    /// </summary>
    public class GlobalUsageAnalytics
    {
        /// <summary>
        /// Gets or sets the total request count.
        /// </summary>
        public long TotalRequests { get; set; }

        /// <summary>
        /// Gets or sets the total token count.
        /// </summary>
        public long TotalTokens { get; set; }

        /// <summary>
        /// Gets or sets the total spend amount.
        /// </summary>
        public decimal TotalSpend { get; set; }

        /// <summary>
        /// Gets or sets the total organization count.
        /// </summary>
        public int TotalOrganizations { get; set; }

        /// <summary>
        /// Gets or sets the total user count.
        /// </summary>
        public int TotalUsers { get; set; }

        /// <summary>
        /// Gets or sets the active organization count.
        /// </summary>
        public int ActiveOrganizations { get; set; }

        /// <summary>
        /// Gets or sets usage grouped by region.
        /// </summary>
        public required IDictionary<string, RegionUsage> UsageByRegion { get; set; }

        /// <summary>
        /// Gets or sets requests grouped by model.
        /// </summary>
        public required IDictionary<string, long> RequestsByModel { get; set; }

        /// <summary>
        /// Gets or sets requests grouped by provider.
        /// </summary>
        public required IDictionary<string, long> RequestsByProvider { get; set; }

        /// <summary>
        /// Gets or sets the analytics start date.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Gets or sets the analytics end date.
        /// </summary>
        public DateTime EndDate { get; set; }
    }

    /// <summary>
    /// Represents usage statistics for a region.
    /// </summary>
    public class RegionUsage
    {
        /// <summary>
        /// Gets or sets the region identifier.
        /// </summary>
        public required string Region { get; set; }

        /// <summary>
        /// Gets or sets the request count.
        /// </summary>
        public long Requests { get; set; }

        /// <summary>
        /// Gets or sets the token count.
        /// </summary>
        public long Tokens { get; set; }

        /// <summary>
        /// Gets or sets the spend amount.
        /// </summary>
        public decimal Spend { get; set; }

        /// <summary>
        /// Gets or sets the organization count.
        /// </summary>
        public int Organizations { get; set; }

        /// <summary>
        /// Gets or sets the user count.
        /// </summary>
        public int Users { get; set; }
    }

    /// <summary>
    /// Represents a cross-border transfer report.
    /// </summary>
    public class CrossBorderTransferReport
    {
        /// <summary>
        /// Gets or sets the transfer identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the organization identifier.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the organization name.
        /// </summary>
        public required string OrganizationName { get; set; }

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the user email.
        /// </summary>
        public required string UserEmail { get; set; }

        /// <summary>
        /// Gets or sets the source region.
        /// </summary>
        public required string FromRegion { get; set; }

        /// <summary>
        /// Gets or sets the destination region.
        /// </summary>
        public required string ToRegion { get; set; }

        /// <summary>
        /// Gets or sets the legal basis for transfer.
        /// </summary>
        public required string LegalBasis { get; set; }

        /// <summary>
        /// Gets or sets the purpose of transfer.
        /// </summary>
        public required string Purpose { get; set; }

        /// <summary>
        /// Gets or sets the data categories transferred.
        /// </summary>
        public required string[] DataCategories { get; set; }

        /// <summary>
        /// Gets or sets the transfer timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Represents the compliance status dashboard.
    /// </summary>
    public class ComplianceStatusDashboard
    {
        /// <summary>
        /// Gets or sets the total organization count.
        /// </summary>
        public int TotalOrganizations { get; set; }

        /// <summary>
        /// Gets or sets the compliant organization count.
        /// </summary>
        public int CompliantOrganizations { get; set; }

        /// <summary>
        /// Gets or sets the count of organizations with issues.
        /// </summary>
        public int OrganizationsWithIssues { get; set; }

        /// <summary>
        /// Gets or sets compliance status grouped by region.
        /// </summary>
        public required IDictionary<string, RegionCompliance> ComplianceByRegion { get; set; }

        /// <summary>
        /// Gets or sets the list of compliance issues.
        /// </summary>
        public required IList<ComplianceIssue> Issues { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when compliance was checked.
        /// </summary>
        public DateTime CheckedAt { get; set; }
    }

    /// <summary>
    /// Represents compliance status for a region.
    /// </summary>
    public class RegionCompliance
    {
        /// <summary>
        /// Gets or sets the region identifier.
        /// </summary>
        public required string Region { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the region is compliant.
        /// </summary>
        public bool IsCompliant { get; set; }

        /// <summary>
        /// Gets or sets the total organization count.
        /// </summary>
        public int TotalOrganizations { get; set; }

        /// <summary>
        /// Gets or sets the count of organizations with consent.
        /// </summary>
        public int OrganizationsWithConsent { get; set; }

        /// <summary>
        /// Gets or sets the cross-border transfer count.
        /// </summary>
        public int CrossBorderTransfers { get; set; }

        /// <summary>
        /// Gets or sets the list of compliance issues.
        /// </summary>
        public required IList<string> Issues { get; set; }
    }

    /// <summary>
    /// Represents a compliance issue.
    /// </summary>
    public class ComplianceIssue
    {
        /// <summary>
        /// Gets or sets the severity (Critical, High, Medium, Low).
        /// </summary>
        public required string Severity { get; set; }

        /// <summary>
        /// Gets or sets the issue category.
        /// </summary>
        public required string Category { get; set; }

        /// <summary>
        /// Gets or sets the issue description.
        /// </summary>
        public required string Description { get; set; }

        /// <summary>
        /// Gets or sets the organization identifier.
        /// </summary>
        public Guid? OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the organization name.
        /// </summary>
        public required string OrganizationName { get; set; }

        /// <summary>
        /// Gets or sets the region.
        /// </summary>
        public required string Region { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when issue was detected.
        /// </summary>
        public DateTime DetectedAt { get; set; }
    }

    /// <summary>
    /// Represents the system health overview.
    /// </summary>
    public class SystemHealthOverview
    {
        /// <summary>
        /// Gets or sets health status grouped by region.
        /// </summary>
        public required IDictionary<string, RegionHealth> HealthByRegion { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether all regions are healthy.
        /// </summary>
        public bool AllRegionsHealthy { get; set; }

        /// <summary>
        /// Gets or sets the total region count.
        /// </summary>
        public int TotalRegions { get; set; }

        /// <summary>
        /// Gets or sets the healthy region count.
        /// </summary>
        public int HealthyRegions { get; set; }

        /// <summary>
        /// Gets or sets the list of system alerts.
        /// </summary>
        public required IList<SystemAlert> Alerts { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when health was checked.
        /// </summary>
        public DateTime CheckedAt { get; set; }
    }

    /// <summary>
    /// Represents a system alert.
    /// </summary>
    public class SystemAlert
    {
        /// <summary>
        /// Gets or sets the severity (Critical, Warning, Info).
        /// </summary>
        public required string Severity { get; set; }

        /// <summary>
        /// Gets or sets the alert message.
        /// </summary>
        public required string Message { get; set; }

        /// <summary>
        /// Gets or sets the region.
        /// </summary>
        public required string Region { get; set; }

        /// <summary>
        /// Gets or sets the alert timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Represents a request to modify organization limits.
    /// </summary>
    public class LimitModificationRequest
    {
        /// <summary>
        /// Gets or sets the organization identifier.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the limit type (MaxTeams, MaxUsersPerTeam, MaxKeysPerUser, etc.).
        /// </summary>
        public required string LimitType { get; set; }

        /// <summary>
        /// Gets or sets the new limit value.
        /// </summary>
        public int NewValue { get; set; }

        /// <summary>
        /// Gets or sets the justification for modification.
        /// </summary>
        public required string Justification { get; set; }

        /// <summary>
        /// Gets or sets the approver identifier.
        /// </summary>
        public required string ApprovedBy { get; set; }
    }

    /// <summary>
    /// Represents the context for super admin access validation.
    /// </summary>
    public class SuperAdminAccessContext
    {
        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the IP address.
        /// </summary>
        public required string IpAddress { get; set; }

        /// <summary>
        /// Gets or sets the MFA code.
        /// </summary>
        public required string MfaCode { get; set; }

        /// <summary>
        /// Gets or sets the action being performed.
        /// </summary>
        public required string Action { get; set; }

        /// <summary>
        /// Gets or sets the justification for action.
        /// </summary>
        public required string Justification { get; set; }

        /// <summary>
        /// Gets or sets the request timestamp.
        /// </summary>
        public DateTime RequestTime { get; set; }
    }

    /// <summary>
    /// Represents the result of super admin access validation.
    /// </summary>
    public class SuperAdminAccessValidation
    {
        /// <summary>
        /// Gets or sets a value indicating whether access is valid.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the failure reason.
        /// </summary>
        public string? FailureReason { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether MFA is required.
        /// </summary>
        public bool MfaRequired { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether MFA is valid.
        /// </summary>
        public bool MfaValid { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether IP is allowed.
        /// </summary>
        public bool IpAllowed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether access is within business hours.
        /// </summary>
        public bool WithinBusinessHours { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether justification is required.
        /// </summary>
        public bool JustificationRequired { get; set; }

        /// <summary>
        /// Gets or sets the validation timestamp.
        /// </summary>
        public DateTime ValidatedAt { get; set; }
    }
}
