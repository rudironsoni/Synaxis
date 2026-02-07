// <copyright file="ProviderTool.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Agents.Tools
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane;

    /// <summary>
    /// Tool for managing provider configurations.
    /// </summary>
    public class ProviderTool : IProviderTool
    {
        private readonly ControlPlaneDbContext _db;
        private readonly ILogger<ProviderTool> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProviderTool"/> class.
        /// </summary>
        /// <param name="db">The database context.</param>
        /// <param name="logger">The logger.</param>
        public ProviderTool(ControlPlaneDbContext db, ILogger<ProviderTool> logger)
        {
            this._db = db;
            this._logger = logger;
        }

        /// <summary>
        /// Updates a provider configuration setting.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="providerId">The provider ID.</param>
        /// <param name="key">The configuration key.</param>
        /// <param name="value">The configuration value.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public async Task<bool> UpdateProviderConfigAsync(Guid organizationId, Guid providerId, string key, object value, CancellationToken ct = default)
        {
            try
            {
                // This is a placeholder - actual implementation would update OrganizationProvider settings
                this._logger.LogInformation("UpdateProviderConfig: OrgId={OrgId}, ProviderId={ProviderId}, Key={Key}",
                    organizationId, providerId, key);
                return true;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Failed to update provider config");
                return false;
            }
        }

        /// <summary>
        /// Gets the status of a provider.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="providerId">The provider ID.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The provider status.</returns>
        public async Task<ProviderStatus> GetProviderStatusAsync(Guid organizationId, Guid providerId, CancellationToken ct = default)
        {
            try
            {
                // Query from Operations schema - ProviderHealthStatus
                var healthStatus = await this._db.Database.SqlQuery<ProviderHealthStatusDto>(
                    $"SELECT \"IsHealthy\", \"LastCheckedAt\" FROM operations.\"ProviderHealthStatus\" WHERE \"OrganizationProviderId\" = {providerId} ORDER BY \"LastCheckedAt\" DESC LIMIT 1").FirstOrDefaultAsync(ct);

                return new ProviderStatus(
                    true, // IsEnabled - would need to query OrganizationProvider
                    healthStatus?.IsHealthy ?? true,
                    healthStatus?.LastCheckedAt);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Failed to get provider status");
                return new ProviderStatus(false, false, null);
            }
        }

        /// <summary>
        /// Gets all providers for an organization.
        /// </summary>
        /// <param name="organizationId">The organization ID.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>List of provider information.</returns>
        public async Task<List<ProviderInfo>> GetAllProvidersAsync(Guid organizationId, CancellationToken ct = default)
        {
            try
            {
                // This would query OrganizationProvider from Operations schema
                return new List<ProviderInfo>();
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Failed to get all providers");
                return new List<ProviderInfo>();
            }
        }

        private sealed class ProviderHealthStatusDto
        {
            public bool IsHealthy { get; set; }

            public DateTime LastCheckedAt { get; set; }
        }
    }
}
