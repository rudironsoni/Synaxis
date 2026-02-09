// <copyright file="OrganizationSettingsService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Synaxis.Core.Contracts;
    using Synaxis.Core.Models;
    using Synaxis.Infrastructure.Data;

    /// <summary>
    /// Service for managing organization settings and limits.
    /// </summary>
    public class OrganizationSettingsService : IOrganizationSettingsService
    {
        private readonly SynaxisDbContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrganizationSettingsService"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        public OrganizationSettingsService(SynaxisDbContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc/>
        public async Task<OrganizationLimitsResponse> GetOrganizationLimitsAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            var organization = await this.context.Organizations
                .FindAsync(new object[] { organizationId }, cancellationToken)
                .ConfigureAwait(false);

            if (organization == null)
            {
                throw new KeyNotFoundException($"Organization with ID {organizationId} not found.");
            }

            return new OrganizationLimitsResponse
            {
                MaxTeams = organization.MaxTeams,
                MaxUsersPerTeam = organization.MaxUsersPerTeam,
                MaxKeysPerUser = organization.MaxKeysPerUser,
                MaxConcurrentRequests = organization.MaxConcurrentRequests,
                MonthlyRequestLimit = organization.MonthlyRequestLimit,
                MonthlyTokenLimit = organization.MonthlyTokenLimit,
            };
        }

        /// <inheritdoc/>
        public async Task<OrganizationLimitsResponse> UpdateOrganizationLimitsAsync(
            Guid organizationId,
            UpdateOrganizationLimitsRequest request,
            Guid updatedByUserId,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            this.ValidateLimitsRequest(request);

            var organization = await this.context.Organizations
                .FindAsync(new object[] { organizationId }, cancellationToken)
                .ConfigureAwait(false);

            if (organization == null)
            {
                throw new KeyNotFoundException($"Organization with ID {organizationId} not found.");
            }

            // Update limits only if provided in request
            if (request.MaxTeams.HasValue)
            {
                organization.MaxTeams = request.MaxTeams.Value;
            }

            if (request.MaxUsersPerTeam.HasValue)
            {
                organization.MaxUsersPerTeam = request.MaxUsersPerTeam.Value;
            }

            if (request.MaxKeysPerUser.HasValue)
            {
                organization.MaxKeysPerUser = request.MaxKeysPerUser.Value;
            }

            if (request.MaxConcurrentRequests.HasValue)
            {
                organization.MaxConcurrentRequests = request.MaxConcurrentRequests.Value;
            }

            if (request.MonthlyRequestLimit.HasValue)
            {
                organization.MonthlyRequestLimit = request.MonthlyRequestLimit.Value;
            }

            if (request.MonthlyTokenLimit.HasValue)
            {
                organization.MonthlyTokenLimit = request.MonthlyTokenLimit.Value;
            }

            organization.UpdatedAt = DateTime.UtcNow;

            await this.context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new OrganizationLimitsResponse
            {
                MaxTeams = organization.MaxTeams,
                MaxUsersPerTeam = organization.MaxUsersPerTeam,
                MaxKeysPerUser = organization.MaxKeysPerUser,
                MaxConcurrentRequests = organization.MaxConcurrentRequests,
                MonthlyRequestLimit = organization.MonthlyRequestLimit,
                MonthlyTokenLimit = organization.MonthlyTokenLimit,
            };
        }

        /// <inheritdoc/>
        public async Task<OrganizationSettingsResponse> GetOrganizationSettingsAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            var organization = await this.context.Organizations
                .FindAsync(new object[] { organizationId }, cancellationToken)
                .ConfigureAwait(false);

            if (organization == null)
            {
                throw new KeyNotFoundException($"Organization with ID {organizationId} not found.");
            }

            return new OrganizationSettingsResponse
            {
                Tier = organization.Tier,
                DataRetentionDays = organization.DataRetentionDays,
                RequireSso = organization.RequireSso,
                AllowedEmailDomains = organization.AllowedEmailDomains,
                AvailableRegions = organization.AvailableRegions,
                PrivacyConsent = organization.PrivacyConsent,
            };
        }

        /// <inheritdoc/>
        public async Task<OrganizationSettingsResponse> UpdateOrganizationSettingsAsync(
            Guid organizationId,
            UpdateOrganizationSettingsRequest request,
            Guid updatedByUserId,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            this.ValidateSettingsRequest(request);

            var organization = await this.context.Organizations
                .FindAsync(new object[] { organizationId }, cancellationToken)
                .ConfigureAwait(false);

            if (organization == null)
            {
                throw new KeyNotFoundException($"Organization with ID {organizationId} not found.");
            }

            // Update settings only if provided in request
            if (request.DataRetentionDays.HasValue)
            {
                organization.DataRetentionDays = request.DataRetentionDays.Value;
            }

            if (request.RequireSso.HasValue)
            {
                organization.RequireSso = request.RequireSso.Value;
            }

            if (request.AllowedEmailDomains != null)
            {
                organization.AllowedEmailDomains = request.AllowedEmailDomains;
            }

            organization.UpdatedAt = DateTime.UtcNow;

            await this.context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new OrganizationSettingsResponse
            {
                Tier = organization.Tier,
                DataRetentionDays = organization.DataRetentionDays,
                RequireSso = organization.RequireSso,
                AllowedEmailDomains = organization.AllowedEmailDomains,
                AvailableRegions = organization.AvailableRegions,
                PrivacyConsent = organization.PrivacyConsent,
            };
        }

        private void ValidateLimitsRequest(UpdateOrganizationLimitsRequest request)
        {
            if (request.MaxTeams.HasValue && request.MaxTeams.Value < 0)
            {
                throw new ArgumentException("MaxTeams must be non-negative.", nameof(request));
            }

            if (request.MaxUsersPerTeam.HasValue && request.MaxUsersPerTeam.Value < 0)
            {
                throw new ArgumentException("MaxUsersPerTeam must be non-negative.", nameof(request));
            }

            if (request.MaxKeysPerUser.HasValue && request.MaxKeysPerUser.Value < 0)
            {
                throw new ArgumentException("MaxKeysPerUser must be non-negative.", nameof(request));
            }

            if (request.MaxConcurrentRequests.HasValue && request.MaxConcurrentRequests.Value < 0)
            {
                throw new ArgumentException("MaxConcurrentRequests must be non-negative.", nameof(request));
            }

            if (request.MonthlyRequestLimit.HasValue && request.MonthlyRequestLimit.Value < 0)
            {
                throw new ArgumentException("MonthlyRequestLimit must be non-negative.", nameof(request));
            }

            if (request.MonthlyTokenLimit.HasValue && request.MonthlyTokenLimit.Value < 0)
            {
                throw new ArgumentException("MonthlyTokenLimit must be non-negative.", nameof(request));
            }
        }

        private void ValidateSettingsRequest(UpdateOrganizationSettingsRequest request)
        {
            if (request.DataRetentionDays.HasValue)
            {
                if (request.DataRetentionDays.Value < 1)
                {
                    throw new ArgumentException("DataRetentionDays must be at least 1.", nameof(request));
                }

                if (request.DataRetentionDays.Value > 3650)
                {
                    throw new ArgumentException("DataRetentionDays cannot exceed 3650 (10 years).", nameof(request));
                }
            }
        }
    }
}
