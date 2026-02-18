// <copyright file="ComplianceService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services.SuperAdmin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Synaxis.Core.Contracts;
    using Synaxis.Infrastructure.Data;

    /// <summary>
    /// Service for compliance operations.
    /// </summary>
    public class ComplianceService : IComplianceService
    {
        private readonly SynaxisDbContext _context;
        private readonly ILogger<ComplianceService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComplianceService"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="logger">The logger.</param>
        public ComplianceService(SynaxisDbContext context, ILogger<ComplianceService> logger)
        {
            this._context = context ?? throw new ArgumentNullException(nameof(context));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                    OrganizationName = r.Organization != null ? r.Organization.Name : string.Empty,
                    UserId = r.UserId,
                    UserEmail = r.User != null ? r.User.Email : string.Empty,
                    FromRegion = r.UserRegion ?? string.Empty,
                    ToRegion = r.ProcessedRegion ?? string.Empty,
                    LegalBasis = r.TransferLegalBasis,
                    Purpose = r.TransferPurpose,
                    DataCategories = new[] { "request_data", "response_data" },
                    Timestamp = r.TransferTimestamp ?? r.CreatedAt,
                })
                .ToListAsync()
                .ConfigureAwait(false);

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
                .CountAsync()
                .ConfigureAwait(false);

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
                .ToDictionaryAsync(r => r.Region)
                .ConfigureAwait(false);

            var issues = new List<ComplianceIssue>();

            var orgsWithoutConsent = await this._context.Organizations
                .Where(o => !o.Users.Any(u => u.CrossBorderConsentGiven) &&
                           o.Users.Any(u => !string.Equals(u.DataResidencyRegion, o.PrimaryRegion, StringComparison.Ordinal)))
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
                .ToListAsync()
                .ConfigureAwait(false);

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
    }
}
