// <copyright file="OrganizationLimitService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services.SuperAdmin
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Synaxis.Core.Contracts;
    using Synaxis.Core.Models;
    using Synaxis.Infrastructure.Data;

    /// <summary>
    /// Service for organization limit operations.
    /// </summary>
    public class OrganizationLimitService : IOrganizationLimitService
    {
        private readonly SynaxisDbContext _context;
        private readonly IAuditService _auditService;
        private readonly ILogger<OrganizationLimitService> _logger;
        private readonly string _currentRegion;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrganizationLimitService"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="auditService">The audit service.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="currentRegion">The current region.</param>
        public OrganizationLimitService(
            SynaxisDbContext context,
            IAuditService auditService,
            ILogger<OrganizationLimitService> logger,
            string currentRegion = "us-east-1")
        {
            this._context = context ?? throw new ArgumentNullException(nameof(context));
            this._auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._currentRegion = currentRegion;
        }

        /// <inheritdoc/>
        public async Task<bool> ModifyOrganizationLimitsAsync(LimitModificationRequest request)
        {
            ValidateLimitModificationRequest(request);

            this._logger.LogWarning(
                "Modifying limits for organization {OrgId}. Type: {Type}, New Value: {Value}. Justification: {Justification}",
                request.OrganizationId,
                request.LimitType,
                request.NewValue,
                request.Justification);

            var org = await this.GetOrganizationAsync(request.OrganizationId).ConfigureAwait(false);
            UpdateOrganizationLimit(org, request.LimitType, request.NewValue);
            org.UpdatedAt = DateTime.UtcNow;

            await this._context.SaveChangesAsync().ConfigureAwait(false);
            await this.LogLimitModificationAsync(request).ConfigureAwait(false);

            return true;
        }

        private static void ValidateLimitModificationRequest(LimitModificationRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.Justification))
            {
                throw new ArgumentException("Justification is required for limit modification", nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.ApprovedBy))
            {
                throw new ArgumentException("Approval is required for limit modification", nameof(request));
            }
        }

        private async Task<Organization> GetOrganizationAsync(Guid organizationId)
        {
            var org = await this._context.Organizations.FindAsync(organizationId).ConfigureAwait(false);
            if (org == null)
            {
                throw new InvalidOperationException($"Organization {organizationId} not found");
            }

            return org;
        }

        private static void UpdateOrganizationLimit(Organization org, string limitType, int? newValue)
        {
            switch (limitType)
            {
                case "MaxTeams":
                    org.MaxTeams = newValue;
                    break;
                case "MaxUsersPerTeam":
                    org.MaxUsersPerTeam = newValue;
                    break;
                case "MaxKeysPerUser":
                    org.MaxKeysPerUser = newValue;
                    break;
                case "MaxConcurrentRequests":
                    org.MaxConcurrentRequests = newValue;
                    break;
                case "MonthlyRequestLimit":
                    org.MonthlyRequestLimit = newValue;
                    break;
                case "MonthlyTokenLimit":
                    org.MonthlyTokenLimit = newValue;
                    break;
                default:
                    throw new ArgumentException($"Unknown limit type: {limitType}", nameof(limitType));
            }
        }

        private Task LogLimitModificationAsync(LimitModificationRequest request)
        {
            return this._auditService.LogEventAsync(new AuditEvent
            {
                OrganizationId = request.OrganizationId,
                EventType = "SUPER_ADMIN_LIMIT_MODIFICATION",
                EventCategory = "ADMIN",
                Action = "modify_organization_limits",
                ResourceType = "Organization",
                ResourceId = request.OrganizationId.ToString(),
                Metadata = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    { "limit_type", request.LimitType },
                    { "new_value", request.NewValue },
                    { "justification", request.Justification },
                    { "approved_by", request.ApprovedBy },
                },
                Region = this._currentRegion,
            });
        }
    }
}
