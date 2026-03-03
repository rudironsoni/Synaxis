// <copyright file="FallbackOrchestrator.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Routing
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Implements multi-tier fallback orchestration for provider selection.
    /// </summary>
    public class FallbackOrchestrator : IFallbackOrchestrator
    {
        private readonly ISmartRouter smartRouter;
        private readonly IQuotaTracker quotaTracker;
        private readonly IHealthStore healthStore;
        private readonly ILogger<FallbackOrchestrator> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="FallbackOrchestrator"/> class.
        /// </summary>
        /// <param name="smartRouter">The smart router for candidate selection.</param>
        /// <param name="quotaTracker">The quota tracker for checking provider quotas.</param>
        /// <param name="healthStore">The health store for checking provider health.</param>
        /// <param name="logger">The logger for diagnostic information.</param>
        public FallbackOrchestrator(
            ISmartRouter smartRouter,
            IQuotaTracker quotaTracker,
            IHealthStore healthStore,
            ILogger<FallbackOrchestrator> logger)
        {
            this.smartRouter = smartRouter;
            this.quotaTracker = quotaTracker;
            this.healthStore = healthStore;
            this.logger = logger;
        }

        /// <summary>
        /// Executes a request with intelligent multi-tier fallback.
        /// </summary>
        /// <typeparam name="T">The return type of the operation.</typeparam>
        /// <param name="modelId">The model identifier.</param>
        /// <param name="streaming">Whether streaming is required.</param>
        /// <param name="preferredProviderKey">Optional user-preferred provider key.</param>
        /// <param name="operation">The operation to execute with the selected provider.</param>
        /// <param name="tenantId">Optional tenant ID for routing configuration.</param>
        /// <param name="userId">Optional user ID for routing configuration.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The result of the operation.</returns>
        public async Task<T> ExecuteWithFallbackAsync<T>(
            string modelId,
            bool streaming,
            string? preferredProviderKey,
            Func<EnrichedCandidate, Task<T>> operation,
            string? tenantId = null,
            string? userId = null,
            CancellationToken cancellationToken = default)
        {
            var candidates = await this.smartRouter.GetCandidatesAsync(modelId, streaming, cancellationToken).ConfigureAwait(false);
            var candidateList = candidates.ToList();

            if (!string.IsNullOrEmpty(preferredProviderKey))
            {
                var preferredResult = await this.TryPreferredProviderAsync(candidateList, preferredProviderKey, operation, cancellationToken).ConfigureAwait(false);
                if (preferredResult is not null)
                {
                    return preferredResult;
                }
            }

            var freeResult = await this.TryTierAsync(candidateList, provider => provider.IsFree, "free tier", operation, cancellationToken).ConfigureAwait(false);
            if (freeResult is not null)
            {
                return freeResult;
            }

            var paidResult = await this.TryTierAsync(candidateList, provider => !provider.IsFree, "paid", operation, cancellationToken).ConfigureAwait(false);
            if (paidResult is not null)
            {
                return paidResult;
            }

            var emergencyResult = await this.TryEmergencyFallbackAsync(candidateList, operation, cancellationToken).ConfigureAwait(false);
            if (emergencyResult is not null)
            {
                return emergencyResult;
            }

            throw new InvalidOperationException($"All fallback tiers failed for model '{modelId}'. No healthy providers available.");
        }

        private async Task<T?> TryPreferredProviderAsync<T>(
            IReadOnlyCollection<EnrichedCandidate> candidates,
            string preferredProviderKey,
            Func<EnrichedCandidate, Task<T>> operation,
            CancellationToken cancellationToken)
        {
            this.logger.LogInformation("Attempting Tier 1 fallback: User preferred provider '{ProviderKey}'", preferredProviderKey);
            try
            {
                var preferred = candidates.FirstOrDefault(c => string.Equals(c.Key, preferredProviderKey, StringComparison.Ordinal));
                if (preferred == null)
                {
                    return default;
                }

                if (!await this.CanUseProviderAsync(preferred, checkQuota: true, cancellationToken).ConfigureAwait(false))
                {
                    return default;
                }

                this.logger.LogInformation("Using user preferred provider '{ProviderKey}'", preferred.Key);
                return await operation(preferred).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "User preferred provider '{ProviderKey}' failed", preferredProviderKey);
                return default;
            }
        }

        private async Task<T?> TryTierAsync<T>(
            IReadOnlyCollection<EnrichedCandidate> candidates,
            Func<EnrichedCandidate, bool> tierFilter,
            string tierLabel,
            Func<EnrichedCandidate, Task<T>> operation,
            CancellationToken cancellationToken)
        {
            this.logger.LogInformation("Attempting Tier fallback: {Tier} providers", tierLabel);
            foreach (var provider in candidates.Where(tierFilter))
            {
                try
                {
                    if (!await this.CanUseProviderAsync(provider, checkQuota: true, cancellationToken).ConfigureAwait(false))
                    {
                        continue;
                    }

                    this.logger.LogInformation("Using {Tier} provider '{ProviderKey}'", tierLabel, provider.Key);
                    return await operation(provider).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning(ex, "{Tier} provider '{ProviderKey}' failed", tierLabel, provider.Key);
                }
            }

            return default;
        }

        private async Task<T?> TryEmergencyFallbackAsync<T>(
            IReadOnlyCollection<EnrichedCandidate> candidates,
            Func<EnrichedCandidate, Task<T>> operation,
            CancellationToken cancellationToken)
        {
            this.logger.LogInformation("Attempting Tier 4 fallback: Emergency fallback");
            foreach (var provider in candidates)
            {
                try
                {
                    if (!await this.CanUseProviderAsync(provider, checkQuota: false, cancellationToken).ConfigureAwait(false))
                    {
                        continue;
                    }

                    this.logger.LogWarning("Emergency fallback using provider '{ProviderKey}'", provider.Key);
                    return await operation(provider).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning(ex, "Emergency fallback provider '{ProviderKey}' failed", provider.Key);
                }
            }

            return default;
        }

        private async Task<bool> CanUseProviderAsync(EnrichedCandidate provider, bool checkQuota, CancellationToken cancellationToken)
        {
            if (!await this.healthStore.IsHealthyAsync(provider.Key!, cancellationToken).ConfigureAwait(false))
            {
                return false;
            }

            if (!checkQuota)
            {
                return true;
            }

            return await this.quotaTracker.CheckQuotaAsync(provider.Key!, cancellationToken).ConfigureAwait(false);
        }
    }
}
