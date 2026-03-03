// <copyright file="ConfigurationResolver.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Services
{
    using Microsoft.EntityFrameworkCore;
    using Synaxis.InferenceGateway.Application.Configuration;
    using Synaxis.InferenceGateway.Application.Configuration.Models;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane;

    /// <summary>
    /// Implementation of hierarchical configuration resolution service.
    /// Resolves settings in order: User → Group → Organization → Global.
    /// </summary>
    public class ConfigurationResolver : IConfigurationResolver
    {
        private readonly SynaxisDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationResolver"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        public ConfigurationResolver(SynaxisDbContext context)
        {
            this._context = context;
        }

        /// <inheritdoc />
        public async Task<ConfigurationSetting<T>> GetSettingAsync<T>(
            string key,
            Guid? userId = null,
            Guid? organizationId = null,
            CancellationToken cancellationToken = default)
        {
            // NOTE: Implement generic setting storage and retrieval
            // This would require a Settings table or JSONB column in relevant entities
            // For now, return a not found setting
            return new ConfigurationSetting<T>
            {
                Value = default,
                Source = "None",
                Found = false,
            };
        }

        /// <inheritdoc />
        public async Task<RateLimitConfiguration> GetRateLimitsAsync(
            Guid? userId = null,
            Guid? organizationId = null,
            CancellationToken cancellationToken = default)
        {
            // 1. Check user membership for user-specific rate limits
            var userRateLimits = await this.GetUserRateLimitsAsync(userId, organizationId, cancellationToken).ConfigureAwait(false);
            if (userRateLimits != null)
            {
                return userRateLimits;
            }

            // 2. Check primary group for group-level rate limits
            var groupRateLimits = await this.GetGroupRateLimitsAsync(userId, organizationId, cancellationToken).ConfigureAwait(false);
            if (groupRateLimits != null)
            {
                return groupRateLimits;
            }

            // 3. Check organization settings for org-level rate limits
            var orgRateLimits = await this.GetOrganizationRateLimitsAsync(organizationId, cancellationToken).ConfigureAwait(false);
            if (orgRateLimits != null)
            {
                return orgRateLimits;
            }

            // 4. Return global defaults (unlimited)
            return new RateLimitConfiguration
            {
                RequestsPerMinute = null,
                TokensPerMinute = null,
                Source = "Global",
            };
        }

        /// <inheritdoc />
        public async Task<CostConfiguration> GetEffectiveCostPer1MTokensAsync(
            Guid organizationId,
            Guid providerId,
            Guid modelId,
            CancellationToken cancellationToken = default)
        {
            // 1. Check OrganizationModel for model-specific pricing
            var orgModel = await this._context.OrganizationModels
                .FirstOrDefaultAsync(
                    om => om.OrganizationId == organizationId && om.ModelId == modelId,
                    cancellationToken).ConfigureAwait(false);

            if (orgModel?.InputCostPer1MTokens.HasValue == true &&
                orgModel.OutputCostPer1MTokens.HasValue)
            {
                return new CostConfiguration
                {
                    InputCostPer1MTokens = orgModel.InputCostPer1MTokens.Value,
                    OutputCostPer1MTokens = orgModel.OutputCostPer1MTokens.Value,
                    Source = "OrganizationModel",
                };
            }

            // 2. Check OrganizationProvider for provider-specific pricing
            var orgProvider = await this._context.OrganizationProviders
                .FirstOrDefaultAsync(
                    op => op.OrganizationId == organizationId && op.ProviderId == providerId,
                    cancellationToken).ConfigureAwait(false);

            if (orgProvider?.InputCostPer1MTokens.HasValue == true &&
                orgProvider.OutputCostPer1MTokens.HasValue)
            {
                return new CostConfiguration
                {
                    InputCostPer1MTokens = orgProvider.InputCostPer1MTokens.Value,
                    OutputCostPer1MTokens = orgProvider.OutputCostPer1MTokens.Value,
                    Source = "OrganizationProvider",
                };
            }

            // 3. Fall back to provider defaults
            var provider = await this._context.Providers
                .FirstOrDefaultAsync(p => p.Id == providerId, cancellationToken).ConfigureAwait(false);

            if (provider?.DefaultInputCostPer1MTokens.HasValue == true &&
                provider.DefaultOutputCostPer1MTokens.HasValue)
            {
                return new CostConfiguration
                {
                    InputCostPer1MTokens = provider.DefaultInputCostPer1MTokens.Value,
                    OutputCostPer1MTokens = provider.DefaultOutputCostPer1MTokens.Value,
                    Source = "Provider",
                };
            }

            // 4. Return zero cost as ultimate fallback
            return new CostConfiguration
            {
                InputCostPer1MTokens = 0,
                OutputCostPer1MTokens = 0,
                Source = "Default",
            };
        }

        /// <inheritdoc />
        public async Task<bool> ShouldAutoOptimizeAsync(
            Guid? userId = null,
            Guid? organizationId = null,
            CancellationToken cancellationToken = default)
        {
            // 1. Check user membership for user-specific setting
            if (userId.HasValue && organizationId.HasValue)
            {
                var membership = await this._context.UserOrganizationMemberships
                    .FirstOrDefaultAsync(
                        m => m.UserId == userId.Value && m.OrganizationId == organizationId.Value,
                        cancellationToken).ConfigureAwait(false);

                if (membership != null)
                {
                    return membership.AllowAutoOptimization;
                }

                // 2. Check primary group for group-level setting
                if (membership?.PrimaryGroupId.HasValue == true)
                {
                    var group = await this._context.Groups
                        .FirstOrDefaultAsync(
                            g => g.Id == membership.PrimaryGroupId.Value,
                            cancellationToken).ConfigureAwait(false);

                    if (group != null)
                    {
                        return group.AllowAutoOptimization;
                    }
                }
            }

            // 3. Check organization settings for org-level setting
            if (organizationId.HasValue)
            {
                var orgSettings = await this._context.OrganizationSettings
                    .FirstOrDefaultAsync(
                        s => s.OrganizationId == organizationId.Value,
                        cancellationToken).ConfigureAwait(false);

                if (orgSettings != null)
                {
                    return orgSettings.AllowAutoOptimization;
                }
            }

            // 4. Return global default (true)
            return true;
        }

        private async Task<RateLimitConfiguration?> GetUserRateLimitsAsync(
            Guid? userId,
            Guid? organizationId,
            CancellationToken cancellationToken)
        {
            if (!userId.HasValue || !organizationId.HasValue)
            {
                return null;
            }

            var membership = await this._context.UserOrganizationMemberships
                .FirstOrDefaultAsync(
                    m => m.UserId == userId.Value && m.OrganizationId == organizationId.Value,
                    cancellationToken).ConfigureAwait(false);

            if (membership?.RateLimitRpm.HasValue == true || membership?.RateLimitTpm.HasValue == true)
            {
                return new RateLimitConfiguration
                {
                    RequestsPerMinute = membership.RateLimitRpm,
                    TokensPerMinute = membership.RateLimitTpm,
                    Source = "UserMembership",
                };
            }

            return null;
        }

        private async Task<RateLimitConfiguration?> GetGroupRateLimitsAsync(
            Guid? userId,
            Guid? organizationId,
            CancellationToken cancellationToken)
        {
            if (!userId.HasValue || !organizationId.HasValue)
            {
                return null;
            }

            var membership = await this._context.UserOrganizationMemberships
                .FirstOrDefaultAsync(
                    m => m.UserId == userId.Value && m.OrganizationId == organizationId.Value,
                    cancellationToken).ConfigureAwait(false);

            if (membership?.PrimaryGroupId.HasValue != true)
            {
                return null;
            }

            var group = await this._context.Groups
                .FirstOrDefaultAsync(
                    g => g.Id == membership.PrimaryGroupId,
                    cancellationToken).ConfigureAwait(false);

            if (group?.RateLimitRpm.HasValue == true || group?.RateLimitTpm.HasValue == true)
            {
                return new RateLimitConfiguration
                {
                    RequestsPerMinute = group.RateLimitRpm,
                    TokensPerMinute = group.RateLimitTpm,
                    Source = "Group",
                };
            }

            return null;
        }

        private async Task<RateLimitConfiguration?> GetOrganizationRateLimitsAsync(
            Guid? organizationId,
            CancellationToken cancellationToken)
        {
            if (!organizationId.HasValue)
            {
                return null;
            }

            var orgSettings = await this._context.OrganizationSettings
                .FirstOrDefaultAsync(
                    s => s.OrganizationId == organizationId.Value,
                    cancellationToken).ConfigureAwait(false);

            if (orgSettings?.DefaultRateLimitRpm > 0 || orgSettings?.DefaultRateLimitTpm > 0)
            {
                return new RateLimitConfiguration
                {
                    RequestsPerMinute = orgSettings.DefaultRateLimitRpm,
                    TokensPerMinute = orgSettings.DefaultRateLimitTpm,
                    Source = "Organization",
                };
            }

            return null;
        }
    }
}
