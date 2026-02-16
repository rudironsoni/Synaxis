// <copyright file="FailoverService.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Synaxis.Core.Contracts;
    using Synaxis.Core.Models;
    using Synaxis.Infrastructure.Data;

    /// <summary>
    /// Manages automatic failover between regions based on health status.
    /// </summary>
    public class FailoverService : IFailoverService
    {
        private readonly SynaxisDbContext _context;
        private readonly IHealthMonitor _healthMonitor;
        private readonly ILogger<FailoverService> _logger;

        // Regional preference for failover (same compliance regime preferred)
        private static readonly Dictionary<string, List<string>> RegionPreferences = new(StringComparer.Ordinal)
        {
            { "eu-west-1", new List<string> { "eu-west-1", "us-east-1", "sa-east-1" } }, // EU prefers EU, then US, then SA
            { "us-east-1", new List<string> { "us-east-1", "eu-west-1", "sa-east-1" } }, // US prefers US, then EU, then SA
            { "sa-east-1", new List<string> { "sa-east-1", "us-east-1", "eu-west-1" } }, // SA prefers SA, then US, then EU
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="FailoverService"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="healthMonitor">The health monitor.</param>
        /// <param name="logger">The logger.</param>
        public FailoverService(
            SynaxisDbContext context,
            IHealthMonitor healthMonitor,
            ILogger<FailoverService> logger)
        {
            this._context = context ?? throw new ArgumentNullException(nameof(context));
            this._healthMonitor = healthMonitor ?? throw new ArgumentNullException(nameof(healthMonitor));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<FailoverDecision> SelectRegionAsync(Guid organizationId, Guid userId, string primaryRegion)
        {
            if (string.IsNullOrWhiteSpace(primaryRegion))
            {
                throw new ArgumentException("Primary region cannot be null or empty", nameof(primaryRegion));
            }

            try
            {
                // Check if primary region is healthy
                var primaryHealthy = await this._healthMonitor.IsRegionHealthyAsync(primaryRegion).ConfigureAwait(false);

                if (primaryHealthy)
                {
                    return this.CreateHealthyRegionDecision(primaryRegion);
                }

                // Primary is unhealthy, need to failover
                this._logger.LogWarning("Primary region {Region} is unhealthy for org {OrgId}, initiating failover", primaryRegion, organizationId);

                return await this.SelectFailoverTargetAsync(organizationId, userId, primaryRegion).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error selecting region for org {OrgId}", organizationId);

                // Fail safe: return primary region
                return new FailoverDecision
                {
                    SelectedRegion = primaryRegion,
                    IsFailover = false,
                    NeedsCrossBorderConsent = false,
                    Reason = $"Failover decision error: {ex.Message}",
                    HealthyRegions = new List<string>(),
                };
            }
        }

        private FailoverDecision CreateHealthyRegionDecision(string region)
        {
            this._logger.LogDebug("Primary region {Region} is healthy", region);

            return new FailoverDecision
            {
                SelectedRegion = region,
                IsFailover = false,
                NeedsCrossBorderConsent = false,
                Reason = "Primary region healthy",
                HealthyRegions = new List<string> { region },
            };
        }

        private async Task<FailoverDecision> SelectFailoverTargetAsync(Guid organizationId, Guid userId, string primaryRegion)
        {
            // Get organization's available regions
            var org = await this._context.Organizations
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == organizationId).ConfigureAwait(false);

            if (org == null)
            {
                throw new InvalidOperationException($"Organization {organizationId} not found");
            }

            var availableRegions = org.AvailableRegions ?? new List<string> { "eu-west-1", "us-east-1", "sa-east-1" };

            // Remove primary region from candidates
            var candidateRegions = availableRegions.Where(r => !string.Equals(r, primaryRegion, StringComparison.Ordinal)).ToList();

            if (!candidateRegions.Any())
            {
                this._logger.LogError("No failover regions available for org {OrgId}", organizationId);

                return new FailoverDecision
                {
                    SelectedRegion = primaryRegion,
                    IsFailover = false,
                    NeedsCrossBorderConsent = false,
                    Reason = "No failover regions available",
                    HealthyRegions = new List<string>(),
                };
            }

            // Find nearest healthy region
            var targetRegion = await this._healthMonitor.GetNearestHealthyRegionAsync(primaryRegion, candidateRegions).ConfigureAwait(false);

            // Check if user has cross-border consent
            var user = await this._context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId).ConfigureAwait(false);

            var needsConsent = user != null &&
                               !user.CrossBorderConsentGiven &&
!string.Equals(user.DataResidencyRegion, targetRegion, StringComparison.Ordinal);

            // Get all healthy regions for the decision
            var allHealth = await this._healthMonitor.GetAllRegionHealthAsync().ConfigureAwait(false);
            var healthyRegions = allHealth
                .Where(h => h.Value.IsHealthy)
                .Select(h => h.Key)
                .ToList();

            this._logger.LogInformation("Failover decision for org {OrgId}: {PrimaryRegion} -> {TargetRegion}, consent needed: {NeedsConsent}", organizationId, primaryRegion, targetRegion, needsConsent);

            return new FailoverDecision
            {
                SelectedRegion = targetRegion,
                IsFailover = true,
                NeedsCrossBorderConsent = needsConsent,
                Reason = $"Primary region {primaryRegion} unhealthy",
                HealthyRegions = healthyRegions,
            };
        }

        /// <inheritdoc/>
        public async Task<FailoverResult> FailoverAsync(Guid organizationId, Guid userId, string fromRegion, string toRegion)
        {
            if (string.IsNullOrWhiteSpace(fromRegion))
            {
                throw new ArgumentException("From region cannot be null or empty", nameof(fromRegion));
            }

            if (string.IsNullOrWhiteSpace(toRegion))
            {
                throw new ArgumentException("To region cannot be null or empty", nameof(toRegion));
            }

            try
            {
                // Check if target region is healthy
                var targetHealthy = await this._healthMonitor.IsRegionHealthyAsync(toRegion).ConfigureAwait(false);

                if (!targetHealthy)
                {
                    this._logger.LogWarning("Failover target region {ToRegion} is unhealthy for org {OrgId}", toRegion, organizationId);

                    return FailoverResult.Failed($"Target region {toRegion} is unhealthy");
                }

                // Check if user has cross-border consent
                var hasConsent = await this.HasCrossBorderConsentAsync(userId).ConfigureAwait(false);
                var user = await this._context.Users.FindAsync(userId).ConfigureAwait(false);

                if (user != null && !hasConsent && !string.Equals(user.DataResidencyRegion, toRegion, StringComparison.Ordinal))
                {
                    this._logger.LogWarning("User {UserId} needs cross-border consent for failover to {ToRegion}", userId, toRegion);

                    var consentUrl = $"/consent/cross-border?from={fromRegion}&to={toRegion}";
                    return FailoverResult.NeedsConsent(toRegion, consentUrl);
                }

                // Record cross-border transfer for compliance
                await this.RecordCrossBorderTransferAsync(organizationId, userId, fromRegion, toRegion, hasConsent ? "consent" : "necessity").ConfigureAwait(false);

                var message = this.GetFailoverNotificationMessage(fromRegion, toRegion, !hasConsent);

                this._logger.LogInformation("Failover successful for org {OrgId}, user {UserId}: {FromRegion} -> {ToRegion}", organizationId, userId, fromRegion, toRegion);

                return FailoverResult.Succeeded(toRegion, message);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error during failover for org {OrgId}", organizationId);
                return FailoverResult.Failed($"Failover error: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<bool> HasCrossBorderConsentAsync(Guid userId)
        {
            try
            {
                var user = await this._context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == userId).ConfigureAwait(false);

                return user?.CrossBorderConsentGiven ?? false;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error checking cross-border consent for user {UserId}", userId);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task RecordCrossBorderTransferAsync(
            Guid organizationId,
            Guid userId,
            string fromRegion,
            string toRegion,
            string legalBasis)
        {
            try
            {
                // In a real implementation, this would insert into the cross_border_transfers table
                // For now, we'll log it
                this._logger.LogInformation("Cross-border transfer recorded: Org {OrgId}, User {UserId}, {FromRegion} -> {ToRegion}, Basis: {LegalBasis}", organizationId, userId, fromRegion, toRegion, legalBasis);

                // This would typically execute SQL like:
                // INSERT INTO cross_border_transfers (organization_id, user_id, from_region, to_region, legal_basis, ...)
                // But we don't have the DbSet configured in the context yet
                await Task.CompletedTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error recording cross-border transfer");
            }
        }

        /// <inheritdoc/>
        public async Task<bool> CanRecoverToPrimaryAsync(string region)
        {
            try
            {
                // Check if primary region has been healthy for a sustained period
                var health = await this._healthMonitor.CheckRegionHealthAsync(region).ConfigureAwait(false);

                // Region should have high health score (>= 90) to recover
                var canRecover = health.IsHealthy && health.HealthScore >= 90;

                if (canRecover)
                {
                    this._logger.LogInformation("Region {Region} has recovered and can accept traffic (score: {Score})", region, health.HealthScore);
                }

                return canRecover;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error checking if region {Region} can recover", region);
                return false;
            }
        }

        /// <inheritdoc/>
        public string GetFailoverNotificationMessage(string fromRegion, string toRegion, bool needsConsent)
        {
            var regionNames = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "eu-west-1", "Europe (Ireland)" },
                { "us-east-1", "US East (Virginia)" },
                { "sa-east-1", "South America (São Paulo)" },
            };

            var fromName = regionNames.GetValueOrDefault(fromRegion, fromRegion);
            var toName = regionNames.GetValueOrDefault(toRegion, toRegion);

            if (needsConsent)
            {
                return $"Service temporarily unavailable in {fromName}. " +
                       $"Your request can be processed in {toName}, but this requires your consent for cross-border data transfer. " +
                       $"Please review and accept the data transfer terms to continue.";
            }

            return $"Service temporarily unavailable in {fromName}. " +
                   $"Your request is being automatically routed to {toName}. " +
                   $"We'll restore service to your primary region as soon as possible.";
        }
    }
}
