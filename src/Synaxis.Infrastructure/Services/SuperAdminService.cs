// <copyright file="SuperAdminService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Synaxis.Core.Contracts;
    using Synaxis.Core.Models;
    using Synaxis.Infrastructure.Data;

    /// <summary>
    /// Super Admin service with cross-region visibility and strict access controls.
    /// </summary>
    public class SuperAdminService : ISuperAdminService
    {
        private readonly SynaxisDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAuditService _auditService;
        private readonly IUserService _userService;
        private readonly ILogger<SuperAdminService> _logger;
        private readonly string _currentRegion;

        // Configuration - should come from IConfiguration in production
        private readonly Dictionary<string, string> _regionEndpoints = new(StringComparer.Ordinal)
        {
            { "us-east-1", "https://api-us.synaxis.io" },
            { "eu-west-1", "https://api-eu.synaxis.io" },
            { "sa-east-1", "https://api-br.synaxis.io" },
        };

        private readonly HashSet<string> _allowedIpRanges = new(StringComparer.Ordinal)
        {
            "10.0.0.0/8",
            "172.16.0.0/12",
            "192.168.0.0/16",
        };

        private readonly int _businessHoursStart = 8; // 8 AM
        private readonly int _businessHoursEnd = 18; // 6 PM

        /// <summary>
        /// Initializes a new instance of the <see cref="SuperAdminService"/> class.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="httpClientFactory"></param>
        /// <param name="auditService"></param>
        /// <param name="userService"></param>
        /// <param name="logger"></param>
        /// <param name="currentRegion"></param>
        public SuperAdminService(
            SynaxisDbContext context,
            IHttpClientFactory httpClientFactory,
            IAuditService auditService,
            IUserService userService,
            ILogger<SuperAdminService> logger,
            string currentRegion = "us-east-1")
        {
            this._context = context ?? throw new ArgumentNullException(nameof(context));
            this._httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            this._auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            this._userService = userService ?? throw new ArgumentNullException(nameof(userService));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._currentRegion = currentRegion;
        }

        /// <inheritdoc/>
        public async Task<IList<OrganizationSummary>> GetCrossRegionOrganizationsAsync()
        {
            this._logger.LogInformation("Fetching cross-region organizations");

            // Get local organizations
            var localOrgs = await this.GetLocalOrganizationsAsync().ConfigureAwait(false);

            // Fetch from all other regions in parallel
            var otherRegions = this._regionEndpoints.Keys.Where(r => !string.Equals(r, this._currentRegion, StringComparison.Ordinal)).ToList();
            var tasks = otherRegions.Select(region => this.FetchOrganizationsFromRegionAsync(region));
            var remoteResults = await Task.WhenAll(tasks).ConfigureAwait(false);

            // Combine all results
            var allOrgs = localOrgs.Concat(remoteResults.SelectMany(r => r)).ToList();

            this._logger.LogInformation(
                "Retrieved {Count} organizations across {Regions} regions",
                allOrgs.Count, this._regionEndpoints.Count);

            return allOrgs;
        }

        /// <inheritdoc/>
        public async Task<ImpersonationToken> GenerateImpersonationTokenAsync(ImpersonationRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.Justification))
            {
                throw new ArgumentException("Justification is required for impersonation");
            }

            if (string.IsNullOrWhiteSpace(request.ApprovedBy))
            {
                throw new ArgumentException("Approval is required for impersonation");
            }

            this._logger.LogWarning(
                "Generating impersonation token for user {UserId} in org {OrgId}. Justification: {Justification}",
                request.UserId, request.OrganizationId, request.Justification);

            // Verify user exists
            var user = await this._context.Users
                .Include(u => u.Organization)
                .FirstOrDefaultAsync(u => u.Id == request.UserId && u.OrganizationId == request.OrganizationId).ConfigureAwait(false);

            if (user == null)
            {
                throw new InvalidOperationException($"User {request.UserId} not found in organization {request.OrganizationId}");
            }

            // Generate secure token
            var tokenData = new
            {
                UserId = request.UserId,
                OrganizationId = request.OrganizationId,
                IssuedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(request.DurationMinutes),
                Justification = request.Justification,
                ApprovedBy = request.ApprovedBy,
                Type = "Impersonation",
            };

            var token = this.GenerateSecureToken(tokenData);

            // Audit log the impersonation
            await this._auditService.LogEventAsync(new AuditEvent
            {
                OrganizationId = request.OrganizationId,
                UserId = request.UserId,
                EventType = "SUPER_ADMIN_IMPERSONATION",
                EventCategory = "SECURITY",
                Action = "generate_impersonation_token",
                ResourceType = "User",
                ResourceId = request.UserId.ToString(),
                Metadata = new Dictionary<string, object>(
StringComparer.Ordinal)
                {
                    { "justification", request.Justification },
                    { "approved_by", request.ApprovedBy },
                    { "duration_minutes", request.DurationMinutes },
                    { "expires_at", tokenData.ExpiresAt },
                },
                Region = this._currentRegion,
            }).ConfigureAwait(false);

            return new ImpersonationToken
            {
                Token = token,
                UserId = request.UserId,
                OrganizationId = request.OrganizationId,
                ExpiresAt = tokenData.ExpiresAt,
                Justification = request.Justification,
            };
        }

        /// <inheritdoc/>
        public async Task<GlobalUsageAnalytics> GetGlobalUsageAnalyticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            this._logger.LogInformation("Fetching global usage analytics from {Start} to {End}", start, end);

            // Get local usage
            var localUsage = await this.GetLocalUsageAsync(start, end).ConfigureAwait(false);

            // Fetch from all other regions in parallel
            var otherRegions = this._regionEndpoints.Keys.Where(r => !string.Equals(r, this._currentRegion, StringComparison.Ordinal)).ToList();
            var tasks = otherRegions.Select(region => this.FetchUsageFromRegionAsync(region, start, end));
            var remoteResults = await Task.WhenAll(tasks).ConfigureAwait(false);

            // Aggregate all usage
            var usageByRegion = new Dictionary<string, RegionUsage>(
StringComparer.Ordinal)
            {
                { this._currentRegion, localUsage },
            };

            foreach (var result in remoteResults.Where(r => r != null && r.Region != null))
            {
                usageByRegion[result.Region] = result;
            }

            var totalRequests = usageByRegion.Values.Sum(u => u.Requests);
            var totalTokens = usageByRegion.Values.Sum(u => u.Tokens);
            var totalSpend = usageByRegion.Values.Sum(u => u.Spend);

            // Get model and provider breakdowns from local region only (for simplicity)
            var requestsByModel = await this._context.Requests
                .Where(r => r.CreatedAt >= start && r.CreatedAt <= end)
                .GroupBy(r => r.Model)
                .Select(g => new { Model = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Model ?? "unknown", x => (long)x.Count).ConfigureAwait(false);

            var requestsByProvider = await this._context.Requests
                .Where(r => r.CreatedAt >= start && r.CreatedAt <= end)
                .GroupBy(r => r.Provider)
                .Select(g => new { Provider = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Provider ?? "unknown", x => (long)x.Count).ConfigureAwait(false);

            return new GlobalUsageAnalytics
            {
                TotalRequests = totalRequests,
                TotalTokens = totalTokens,
                TotalSpend = totalSpend,
                TotalOrganizations = await this._context.Organizations.CountAsync().ConfigureAwait(false),
                TotalUsers = await this._context.Users.CountAsync(u => u.IsActive).ConfigureAwait(false),
                ActiveOrganizations = await this._context.Organizations.CountAsync(o => o.IsActive).ConfigureAwait(false),
                UsageByRegion = usageByRegion,
                RequestsByModel = requestsByModel,
                RequestsByProvider = requestsByProvider,
                StartDate = start,
                EndDate = end,
            };
        }

        /// <inheritdoc/>
        public async Task<IList<CrossBorderTransferReport>> GetCrossBorderTransfersAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            this._logger.LogInformation("Fetching cross-border transfers from {Start} to {End}", start, end);

            var transfers = await this._context.Requests
                .Where(r => r.CrossBorderTransfer && r.CreatedAt >= start && r.CreatedAt <= end)
                .Include(r => r.Organization)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new CrossBorderTransferReport
                {
                    Id = r.Id,
                    OrganizationId = r.OrganizationId,
                    OrganizationName = r.Organization.Name,
                    UserId = r.UserId,
                    UserEmail = r.User != null ? r.User.Email : null,
                    FromRegion = r.UserRegion,
                    ToRegion = r.ProcessedRegion,
                    LegalBasis = r.TransferLegalBasis,
                    Purpose = r.TransferPurpose,
                    DataCategories = new[] { "request_data", "response_data" },
                    Timestamp = r.TransferTimestamp ?? r.CreatedAt,
                })
                .ToListAsync().ConfigureAwait(false);

            return transfers;
        }

        /// <inheritdoc/>
        public async Task<ComplianceStatusDashboard> GetComplianceStatusAsync()
        {
            this._logger.LogInformation("Checking compliance status across regions");

            var totalOrgs = await this._context.Organizations.CountAsync().ConfigureAwait(false);
            var orgsWithConsent = await this._context.Users
                .Where(u => u.CrossBorderConsentGiven)
                .Select(u => u.OrganizationId)
                .Distinct()
                .CountAsync().ConfigureAwait(false);

            var complianceByRegion = await this._context.Organizations
                .GroupBy(o => o.PrimaryRegion)
                .Select(g => new RegionCompliance
                {
                    Region = g.Key,
                    IsCompliant = true,
                    TotalOrganizations = g.Count(),
                    OrganizationsWithConsent = g.Count(o => o.Users.Any(u => u.CrossBorderConsentGiven)),
                    CrossBorderTransfers = 0,
                    Issues = new List<string>(),
                })
                .ToDictionaryAsync(r => r.Region).ConfigureAwait(false);

            var issues = new List<ComplianceIssue>();

            // Check for organizations without consent doing cross-border transfers
            var orgsWithoutConsent = await this._context.Organizations
                .Where(o => !o.Users.Any(u => u.CrossBorderConsentGiven) &&
                           o.Users.Any(u => u.DataResidencyRegion != o.PrimaryRegion))
                .Select(o => new ComplianceIssue
                {
                    Severity = "High",
                    Category = "GDPR_CONSENT",
                    Description = "Organization has cross-border data but no user consent",
                    OrganizationId = o.Id,
                    OrganizationName = o.Name,
                    Region = o.PrimaryRegion,
                    DetectedAt = DateTime.UtcNow,
                })
                .ToListAsync().ConfigureAwait(false);

            issues.AddRange(orgsWithoutConsent);

            return new ComplianceStatusDashboard
            {
                TotalOrganizations = totalOrgs,
                CompliantOrganizations = orgsWithConsent,
                OrganizationsWithIssues = issues.Count,
                ComplianceByRegion = complianceByRegion,
                Issues = issues,
                CheckedAt = DateTime.UtcNow,
            };
        }

        /// <inheritdoc/>
        public async Task<SystemHealthOverview> GetSystemHealthOverviewAsync()
        {
            this._logger.LogInformation("Checking system health across regions");

            var healthByRegion = new Dictionary<string, RegionHealth>(StringComparer.Ordinal);
            var alerts = new List<SystemAlert>();

            // Check local region health
            var localHealth = await this.CheckLocalRegionHealthAsync().ConfigureAwait(false);
            healthByRegion[this._currentRegion] = localHealth;

            // Check remote regions in parallel
            var otherRegions = this._regionEndpoints.Keys.Where(r => !string.Equals(r, this._currentRegion, StringComparison.Ordinal)).ToList();
            var tasks = otherRegions.Select(region => this.CheckRemoteRegionHealthAsync(region));
            var remoteHealthResults = await Task.WhenAll(tasks).ConfigureAwait(false);

            foreach (var health in remoteHealthResults.Where(h => h != null))
            {
                healthByRegion[health.Region] = health;

                if (!health.IsHealthy)
                {
                    alerts.Add(new SystemAlert
                    {
                        Severity = string.Equals(health.Status, "Down", StringComparison.Ordinal) ? "Critical" : "Warning",
                        Message = $"Region {health.Region} is {health.Status}",
                        Region = health.Region,
                        Timestamp = DateTime.UtcNow,
                    });
                }
            }

            var healthyRegions = healthByRegion.Values.Count(h => h.IsHealthy);

            return new SystemHealthOverview
            {
                HealthByRegion = healthByRegion,
                AllRegionsHealthy = healthyRegions == healthByRegion.Count,
                TotalRegions = healthByRegion.Count,
                HealthyRegions = healthyRegions,
                Alerts = alerts,
                CheckedAt = DateTime.UtcNow,
            };
        }

        /// <inheritdoc/>
        public async Task<bool> ModifyOrganizationLimitsAsync(LimitModificationRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.Justification))
            {
                throw new ArgumentException("Justification is required for limit modification");
            }

            if (string.IsNullOrWhiteSpace(request.ApprovedBy))
            {
                throw new ArgumentException("Approval is required for limit modification");
            }

            this._logger.LogWarning(
                "Modifying limits for organization {OrgId}. Type: {Type}, New Value: {Value}. Justification: {Justification}",
                request.OrganizationId, request.LimitType, request.NewValue, request.Justification);

            var org = await this._context.Organizations.FindAsync(request.OrganizationId).ConfigureAwait(false);
            if (org == null)
            {
                throw new InvalidOperationException($"Organization {request.OrganizationId} not found");
            }

            // Update the appropriate limit
            switch (request.LimitType)
            {
                case "MaxTeams":
                    org.MaxTeams = request.NewValue;
                    break;
                case "MaxUsersPerTeam":
                    org.MaxUsersPerTeam = request.NewValue;
                    break;
                case "MaxKeysPerUser":
                    org.MaxKeysPerUser = request.NewValue;
                    break;
                case "MaxConcurrentRequests":
                    org.MaxConcurrentRequests = request.NewValue;
                    break;
                case "MonthlyRequestLimit":
                    org.MonthlyRequestLimit = request.NewValue;
                    break;
                case "MonthlyTokenLimit":
                    org.MonthlyTokenLimit = request.NewValue;
                    break;
                default:
                    throw new ArgumentException($"Unknown limit type: {request.LimitType}");
            }

            org.UpdatedAt = DateTime.UtcNow;
            await this._context.SaveChangesAsync().ConfigureAwait(false);

            // Audit log the modification
            await this._auditService.LogEventAsync(new AuditEvent
            {
                OrganizationId = request.OrganizationId,
                EventType = "SUPER_ADMIN_LIMIT_MODIFICATION",
                EventCategory = "ADMIN",
                Action = "modify_organization_limits",
                ResourceType = "Organization",
                ResourceId = request.OrganizationId.ToString(),
                Metadata = new Dictionary<string, object>(
StringComparer.Ordinal)
                {
                    { "limit_type", request.LimitType },
                    { "new_value", request.NewValue },
                    { "justification", request.Justification },
                    { "approved_by", request.ApprovedBy },
                },
                Region = this._currentRegion,
            }).ConfigureAwait(false);

            return true;
        }

        /// <inheritdoc/>
        public async Task<SuperAdminAccessValidation> ValidateAccessAsync(SuperAdminAccessContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var validation = new SuperAdminAccessValidation
            {
                ValidatedAt = DateTime.UtcNow,
                MfaRequired = true,
                JustificationRequired = true,
            };

            // Check if user has SuperAdmin role
            var user = await this._userService.GetUserAsync(context.UserId).ConfigureAwait(false);
            if (!string.Equals(user.Role, "SuperAdmin", StringComparison.Ordinal))
            {
                validation.IsValid = false;
                validation.FailureReason = "User is not a Super Admin";
                return validation;
            }

            // Verify MFA
            if (!user.MfaEnabled)
            {
                validation.IsValid = false;
                validation.MfaValid = false;
                validation.FailureReason = "MFA is not enabled for Super Admin account";
                return validation;
            }

            if (string.IsNullOrWhiteSpace(context.MfaCode))
            {
                validation.IsValid = false;
                validation.MfaValid = false;
                validation.FailureReason = "MFA code is required";
                return validation;
            }

            var mfaValid = await this._userService.VerifyMfaCodeAsync(context.UserId, context.MfaCode).ConfigureAwait(false);
            validation.MfaValid = mfaValid;

            if (!mfaValid)
            {
                validation.IsValid = false;
                validation.FailureReason = "Invalid MFA code";

                // Audit failed MFA attempt
                await this._auditService.LogEventAsync(new AuditEvent
                {
                    OrganizationId = user.OrganizationId,
                    UserId = context.UserId,
                    EventType = "SUPER_ADMIN_MFA_FAILED",
                    EventCategory = "SECURITY",
                    Action = "failed_mfa_verification",
                    ResourceType = "SuperAdmin",
                    ResourceId = context.UserId.ToString(),
                    IpAddress = context.IpAddress,
                    Region = this._currentRegion,
                }).ConfigureAwait(false);

                return validation;
            }

            // Check IP allowlist
            validation.IpAllowed = this.IsIpAllowed(context.IpAddress);
            if (!validation.IpAllowed)
            {
                validation.IsValid = false;
                validation.FailureReason = "IP address not in allowlist";

                // Audit unauthorized IP attempt
                await this._auditService.LogEventAsync(new AuditEvent
                {
                    OrganizationId = user.OrganizationId,
                    UserId = context.UserId,
                    EventType = "SUPER_ADMIN_UNAUTHORIZED_IP",
                    EventCategory = "SECURITY",
                    Action = "unauthorized_ip_attempt",
                    ResourceType = "SuperAdmin",
                    ResourceId = context.UserId.ToString(),
                    IpAddress = context.IpAddress,
                    Region = this._currentRegion,
                }).ConfigureAwait(false);

                return validation;
            }

            // Check justification for sensitive actions (before business hours to fail fast on invalid requests)
            var sensitiveActions = new[] { "impersonate", "modify_limits", "export_pii", "delete_organization" };
            if (sensitiveActions.Contains(context.Action?.ToLowerInvariant()) &&
                string.IsNullOrWhiteSpace(context.Justification))
            {
                validation.IsValid = false;
                validation.FailureReason = "Justification is required for sensitive actions";
                return validation;
            }

            // Check business hours (in UTC)
            var requestTime = context.RequestTime == default ? DateTime.UtcNow : context.RequestTime;
            var currentHour = requestTime.Hour;
            validation.WithinBusinessHours = currentHour >= this._businessHoursStart && currentHour < this._businessHoursEnd;

            if (!validation.WithinBusinessHours)
            {
                validation.IsValid = false;
                validation.FailureReason = "Super Admin access is restricted to business hours (8 AM - 6 PM UTC)";

                // Audit off-hours attempt
                await this._auditService.LogEventAsync(new AuditEvent
                {
                    OrganizationId = user.OrganizationId,
                    UserId = context.UserId,
                    EventType = "SUPER_ADMIN_OFF_HOURS",
                    EventCategory = "SECURITY",
                    Action = "off_hours_access_attempt",
                    ResourceType = "SuperAdmin",
                    ResourceId = context.UserId.ToString(),
                    IpAddress = context.IpAddress,
                    Metadata = new Dictionary<string, object>(
StringComparer.Ordinal)
                    {
                        { "requested_action", context.Action },
                        { "hour_utc", currentHour },
                    },
                    Region = this._currentRegion,
                }).ConfigureAwait(false);

                return validation;
            }

            validation.IsValid = true;

            // Audit successful validation
            await this._auditService.LogEventAsync(new AuditEvent
            {
                OrganizationId = user.OrganizationId,
                UserId = context.UserId,
                EventType = "SUPER_ADMIN_ACCESS_GRANTED",
                EventCategory = "SECURITY",
                Action = "access_validation_success",
                ResourceType = "SuperAdmin",
                ResourceId = context.UserId.ToString(),
                IpAddress = context.IpAddress,
                Metadata = new Dictionary<string, object>(
StringComparer.Ordinal)
                {
                    { "requested_action", context.Action ?? "unknown" },
                    { "justification", context.Justification ?? "none" },
                },
                Region = this._currentRegion,
            }).ConfigureAwait(false);

            return validation;
        }

        // Private helper methods
        private Task<List<OrganizationSummary>> GetLocalOrganizationsAsync()
        {
            return this._context.Organizations
                .Select(o => new OrganizationSummary
                {
                    Id = o.Id,
                    Name = o.Name,
                    Slug = o.Slug,
                    PrimaryRegion = o.PrimaryRegion,
                    Tier = o.Tier,
                    UserCount = o.Users.Count(u => u.IsActive),
                    TeamCount = o.Teams.Count(t => t.IsActive),
                    MonthlyRequests = 0, // Request count calculation requires additional schema changes
                    MonthlySpend = o.CreditBalance, // Using credit balance as placeholder
                    IsActive = o.IsActive,
                    CreatedAt = o.CreatedAt,
                })
                .ToListAsync();
        }

        private async Task<List<OrganizationSummary>> FetchOrganizationsFromRegionAsync(string region)
        {
            try
            {
                using var client = this._httpClientFactory.CreateClient();
                var endpoint = this._regionEndpoints[region];
                using var response = await client.GetAsync($"{endpoint}/api/internal/organizations").ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return JsonSerializer.Deserialize<List<OrganizationSummary>>(json) ?? new List<OrganizationSummary>();
                }

                this._logger.LogWarning("Failed to fetch organizations from region {Region}: {Status}", region, response.StatusCode);
                return new List<OrganizationSummary>();
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error fetching organizations from region {Region}", region);
                return new List<OrganizationSummary>();
            }
        }

        private async Task<RegionUsage> GetLocalUsageAsync(DateTime start, DateTime end)
        {
            var requests = await this._context.Requests
                .Where(r => r.CreatedAt >= start && r.CreatedAt <= end)
                .ToListAsync().ConfigureAwait(false);

            return new RegionUsage
            {
                Region = this._currentRegion,
                Requests = requests.Count,
                Tokens = requests.Sum(r => r.TotalTokens),
                Spend = requests.Sum(r => r.Cost),
                Organizations = await this._context.Organizations.CountAsync().ConfigureAwait(false),
                Users = await this._context.Users.CountAsync(u => u.IsActive)
.ConfigureAwait(false),
            };
        }

        private async Task<RegionUsage> FetchUsageFromRegionAsync(string region, DateTime start, DateTime end)
        {
            try
            {
                using var client = this._httpClientFactory.CreateClient();
                var endpoint = this._regionEndpoints[region];
                using var response = await client.GetAsync($"{endpoint}/api/internal/usage?start={start:O}&end={end:O}").ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return JsonSerializer.Deserialize<RegionUsage>(json);
                }

                this._logger.LogWarning("Failed to fetch usage from region {Region}: {Status}", region, response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error fetching usage from region {Region}", region);
                return null;
            }
        }

        private async Task<RegionHealth> CheckLocalRegionHealthAsync()
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // Simple health check - can database connection be established?
                await this._context.Organizations.AnyAsync().ConfigureAwait(false);

                var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

                return new RegionHealth
                {
                    Region = this._currentRegion,
                    IsHealthy = true,
                    Status = "Healthy",
                    ResponseTimeMs = responseTime,
                    ErrorRate = 0,
                    ActiveConnections = 0, // Would come from monitoring system
                    Version = "1.0.0", // Would come from assembly version
                    LastChecked = DateTime.UtcNow,
                };
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Health check failed for local region {Region}", this._currentRegion);

                return new RegionHealth
                {
                    Region = this._currentRegion,
                    IsHealthy = false,
                    Status = "Down",
                    ResponseTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds,
                    ErrorRate = 1.0,
                    ActiveConnections = 0,
                    Version = "Unknown",
                    LastChecked = DateTime.UtcNow,
                };
            }
        }

        private async Task<RegionHealth> CheckRemoteRegionHealthAsync(string region)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                using var client = this._httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(5);

                var endpoint = this._regionEndpoints[region];
                using var response = await client.GetAsync($"{endpoint}/api/health").ConfigureAwait(false);

                var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

                if (response.IsSuccessStatusCode)
                {
                    return new RegionHealth
                    {
                        Region = region,
                        IsHealthy = true,
                        Status = "Healthy",
                        ResponseTimeMs = responseTime,
                        ErrorRate = 0,
                        ActiveConnections = 0,
                        Version = "1.0.0",
                        LastChecked = DateTime.UtcNow,
                    };
                }

                return new RegionHealth
                {
                    Region = region,
                    IsHealthy = false,
                    Status = "Degraded",
                    ResponseTimeMs = responseTime,
                    ErrorRate = 0.5,
                    ActiveConnections = 0,
                    Version = "Unknown",
                    LastChecked = DateTime.UtcNow,
                };
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Health check failed for region {Region}", region);

                return new RegionHealth
                {
                    Region = region,
                    IsHealthy = false,
                    Status = "Down",
                    ResponseTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds,
                    ErrorRate = 1.0,
                    ActiveConnections = 0,
                    Version = "Unknown",
                    LastChecked = DateTime.UtcNow,
                };
            }
        }

        private string GenerateSecureToken(object tokenData)
        {
            var json = JsonSerializer.Serialize(tokenData);
            var bytes = Encoding.UTF8.GetBytes(json);

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("super-secret-key-should-be-in-config"));
            var hash = hmac.ComputeHash(bytes);

            var tokenBytes = bytes.Concat(hash).ToArray();
            return Convert.ToBase64String(tokenBytes);
        }

        private bool IsIpAllowed(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                return false;
            }

            // Simple check - in production would use proper CIDR matching
            // For now, allow private IP ranges
            return ipAddress.StartsWith("10.", StringComparison.Ordinal) ||
                   ipAddress.StartsWith("172.", StringComparison.Ordinal) ||
                   ipAddress.StartsWith("192.168.", StringComparison.Ordinal) ||
string.Equals(ipAddress, "127.0.0.1", StringComparison.Ordinal) ||
string.Equals(ipAddress, "::1", StringComparison.Ordinal);
        }
    }
}
