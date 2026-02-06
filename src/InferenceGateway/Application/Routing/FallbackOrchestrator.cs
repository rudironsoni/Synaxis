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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "MA0051:Method is too long", Justification = "Multi-tier fallback orchestration requires sequential logic")]
        public async Task<T> ExecuteWithFallbackAsync<T>(
            string modelId,
            bool streaming,
            string? preferredProviderKey,
            Func<EnrichedCandidate, Task<T>> operation,
            string? tenantId = null,
            string? userId = null,
            CancellationToken cancellationToken = default)
        {
            // Tier 1: User preferred provider
            if (!string.IsNullOrEmpty(preferredProviderKey))
            {
                this.logger.LogInformation("Attempting Tier 1 fallback: User preferred provider '{ProviderKey}'", preferredProviderKey);
                try
                {
                    var candidates = await this.smartRouter.GetCandidatesAsync(modelId, streaming, cancellationToken).ConfigureAwait(false);
                    var preferred = candidates.FirstOrDefault(c => string.Equals(c.Key, preferredProviderKey, StringComparison.Ordinal));

                    if (preferred != null &&
                        await this.healthStore.IsHealthyAsync(preferred.Key!, cancellationToken).ConfigureAwait(false) &&
                        await this.quotaTracker.CheckQuotaAsync(preferred.Key!, cancellationToken).ConfigureAwait(false))
                    {
                        this.logger.LogInformation("Using user preferred provider '{ProviderKey}'", preferred.Key);
                        return await operation(preferred).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning(ex, "User preferred provider '{ProviderKey}' failed", preferredProviderKey);
                }
            }

            // Tier 2: Free tier providers
            this.logger.LogInformation("Attempting Tier 2 fallback: Free tier providers");
            try
            {
                var candidates = await this.smartRouter.GetCandidatesAsync(modelId, streaming, cancellationToken).ConfigureAwait(false);
                var freeProviders = candidates.Where(c => c.IsFree).ToList();

                foreach (var provider in freeProviders)
                {
                    try
                    {
                        if (await this.healthStore.IsHealthyAsync(provider.Key!, cancellationToken).ConfigureAwait(false) &&
                            await this.quotaTracker.CheckQuotaAsync(provider.Key!, cancellationToken).ConfigureAwait(false))
                        {
                            this.logger.LogInformation("Using free tier provider '{ProviderKey}'", provider.Key);
                            return await operation(provider).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogWarning(ex, "Free tier provider '{ProviderKey}' failed", provider.Key);
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Free tier fallback failed");
            }

            // Tier 3: Paid providers
            this.logger.LogInformation("Attempting Tier 3 fallback: Paid providers");
            try
            {
                var candidates = await this.smartRouter.GetCandidatesAsync(modelId, streaming, cancellationToken).ConfigureAwait(false);
                var paidProviders = candidates.Where(c => !c.IsFree).ToList();

                foreach (var provider in paidProviders)
                {
                    try
                    {
                        if (await this.healthStore.IsHealthyAsync(provider.Key!, cancellationToken).ConfigureAwait(false) &&
                            await this.quotaTracker.CheckQuotaAsync(provider.Key!, cancellationToken).ConfigureAwait(false))
                        {
                            this.logger.LogInformation("Using paid provider '{ProviderKey}'", provider.Key);
                            return await operation(provider).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogWarning(ex, "Paid provider '{ProviderKey}' failed", provider.Key);
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Paid provider fallback failed");
            }

            // Tier 4: Emergency fallback - any healthy provider
            this.logger.LogInformation("Attempting Tier 4 fallback: Emergency fallback");
            var allCandidates = await this.smartRouter.GetCandidatesAsync(modelId, streaming, cancellationToken).ConfigureAwait(false);
            foreach (var provider in allCandidates)
            {
                try
                {
                    if (await this.healthStore.IsHealthyAsync(provider.Key!, cancellationToken).ConfigureAwait(false))
                    {
                        this.logger.LogWarning("Emergency fallback using provider '{ProviderKey}'", provider.Key);
                        return await operation(provider).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning(ex, "Emergency fallback provider '{ProviderKey}' failed", provider.Key);
                }
            }

            throw new InvalidOperationException($"All fallback tiers failed for model '{modelId}'. No healthy providers available.");
        }
    }
}
