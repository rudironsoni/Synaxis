using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Synaxis.Core.Contracts;
using Synaxis.Core.Models;
using Synaxis.Infrastructure.Data;

namespace Synaxis.Infrastructure.Services
{
    /// <summary>
    /// Service for managing organizations (tenants).
    /// </summary>
    public class TenantService : ITenantService
    {
        private readonly SynaxisDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantService"/> class.
        /// </summary>
        /// <param name="context"></param>
        public TenantService(SynaxisDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Organization> CreateOrganizationAsync(CreateOrganizationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Organization name is required", nameof(request));

            if (string.IsNullOrWhiteSpace(request.Slug))
                throw new ArgumentException("Organization slug is required", nameof(request));

            if (string.IsNullOrWhiteSpace(request.PrimaryRegion))
                throw new ArgumentException("Primary region is required", nameof(request));

            // Check if slug already exists
            var existingOrg = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Slug == request.Slug);

            if (existingOrg != null)
                throw new InvalidOperationException($"Organization with slug '{request.Slug}' already exists");

            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Slug = request.Slug,
                PrimaryRegion = request.PrimaryRegion,
                BillingCurrency = request.BillingCurrency ?? "USD",
                Tier = "free",
                IsActive = true,
                IsVerified = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();

            return organization;
        }

        public async Task<Organization> GetOrganizationAsync(Guid id)
        {
            var organization = await _context.Organizations
                .Include(o => o.Teams)
                .Include(o => o.Users)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (organization == null)
                throw new InvalidOperationException($"Organization with ID '{id}' not found");

            return organization;
        }

        public async Task<Organization> GetOrganizationBySlugAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                throw new ArgumentException("Slug is required", nameof(slug));

            var organization = await _context.Organizations
                .Include(o => o.Teams)
                .Include(o => o.Users)
                .FirstOrDefaultAsync(o => o.Slug == slug);

            if (organization == null)
                throw new InvalidOperationException($"Organization with slug '{slug}' not found");

            return organization;
        }

        public async Task<Organization> UpdateOrganizationAsync(Guid id, UpdateOrganizationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var organization = await _context.Organizations.FindAsync(id);

            if (organization == null)
                throw new InvalidOperationException($"Organization with ID '{id}' not found");

            if (!string.IsNullOrWhiteSpace(request.Name))
                organization.Name = request.Name;

            if (!string.IsNullOrWhiteSpace(request.Description))
                organization.Description = request.Description;

            organization.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return organization;
        }

        public async Task<bool> DeleteOrganizationAsync(Guid id)
        {
            var organization = await _context.Organizations.FindAsync(id);

            if (organization == null)
                return false;

            // Soft delete: set IsActive to false
            organization.IsActive = false;
            organization.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<OrganizationLimits> GetOrganizationLimitsAsync(Guid organizationId)
        {
            var organization = await _context.Organizations.FindAsync(organizationId);

            if (organization == null)
                throw new InvalidOperationException($"Organization with ID '{organizationId}' not found");

            // Get plan defaults
            var plan = await _context.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.Slug == organization.Tier);

            var limits = new OrganizationLimits();

            if (plan != null && plan.LimitsConfig != null)
            {
                // Extract from plan config
                limits.MaxTeams = GetLimitValue<int>(plan.LimitsConfig, "max_teams", 1);
                limits.MaxUsersPerTeam = GetLimitValue<int>(plan.LimitsConfig, "max_users_per_team", 3);
                limits.MaxKeysPerUser = GetLimitValue<int>(plan.LimitsConfig, "max_keys_per_user", 2);
                limits.MaxConcurrentRequests = GetLimitValue<int>(plan.LimitsConfig, "max_concurrent_requests", 10);
                limits.MonthlyRequestLimit = GetLimitValue<long>(plan.LimitsConfig, "monthly_request_limit", 10000);
                limits.MonthlyTokenLimit = GetLimitValue<long>(plan.LimitsConfig, "monthly_token_limit", 100000);
                limits.DataRetentionDays = GetLimitValue<int>(plan.LimitsConfig, "data_retention_days", 30);
            }
            else
            {
                // Default free tier limits
                limits.MaxTeams = 1;
                limits.MaxUsersPerTeam = 3;
                limits.MaxKeysPerUser = 2;
                limits.MaxConcurrentRequests = 10;
                limits.MonthlyRequestLimit = 10000;
                limits.MonthlyTokenLimit = 100000;
                limits.DataRetentionDays = 30;
            }

            // Apply organization overrides
            if (organization.MaxTeams.HasValue)
                limits.MaxTeams = organization.MaxTeams.Value;
            if (organization.MaxUsersPerTeam.HasValue)
                limits.MaxUsersPerTeam = organization.MaxUsersPerTeam.Value;
            if (organization.MaxKeysPerUser.HasValue)
                limits.MaxKeysPerUser = organization.MaxKeysPerUser.Value;
            if (organization.MaxConcurrentRequests.HasValue)
                limits.MaxConcurrentRequests = organization.MaxConcurrentRequests.Value;
            if (organization.MonthlyRequestLimit.HasValue)
                limits.MonthlyRequestLimit = organization.MonthlyRequestLimit.Value;
            if (organization.MonthlyTokenLimit.HasValue)
                limits.MonthlyTokenLimit = organization.MonthlyTokenLimit.Value;

            limits.DataRetentionDays = organization.DataRetentionDays;

            return limits;
        }

        public async Task<bool> CanAddTeamAsync(Guid organizationId)
        {
            var organization = await _context.Organizations
                .Include(o => o.Teams)
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (organization == null)
                return false;

            var limits = await GetOrganizationLimitsAsync(organizationId);

            var currentTeamCount = organization.Teams?.Count(t => t.IsActive) ?? 0;

            return currentTeamCount < limits.MaxTeams;
        }

        public async Task<bool> IsUnderConcurrentLimitAsync(Guid organizationId)
        {
            // This would typically check against a cache/counter of active requests
            // For now, return true as placeholder
            var limits = await GetOrganizationLimitsAsync(organizationId);

            return true;
        }

        private T GetLimitValue<T>(IDictionary<string, object> config, string key, T defaultValue)
        {
            if (config == null || !config.ContainsKey(key))
                return defaultValue;

            var value = config[key];

            if (value == null)
                return defaultValue;

            try
            {
                if (value is T typedValue)
                    return typedValue;

                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}
