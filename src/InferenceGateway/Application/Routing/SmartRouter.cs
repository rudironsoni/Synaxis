// <copyright file="SmartRouter.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Routing
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Synaxis.InferenceGateway.Application.Configuration;

    /// <summary>
    /// Intelligent routing service that selects optimal providers based on scoring.
    /// </summary>
    public class SmartRouter : ISmartRouter
    {
        private readonly IModelResolver modelResolver;
        private readonly ICostService costService;
        private readonly IHealthStore healthStore;
        private readonly IQuotaTracker quotaTracker;
        private readonly IRoutingScoreCalculator routingScoreCalculator;
        private readonly ILogger<SmartRouter> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SmartRouter"/> class.
        /// </summary>
        /// <param name="modelResolver">The model resolver service.</param>
        /// <param name="costService">The cost calculation service.</param>
        /// <param name="healthStore">The health monitoring store.</param>
        /// <param name="quotaTracker">The quota tracking service.</param>
        /// <param name="routingScoreCalculator">The routing score calculator.</param>
        /// <param name="logger">The logger instance.</param>
        public SmartRouter(
            IModelResolver modelResolver,
            ICostService costService,
            IHealthStore healthStore,
            IQuotaTracker quotaTracker,
            IRoutingScoreCalculator routingScoreCalculator,
            ILogger<SmartRouter> logger)
        {
            this.modelResolver = modelResolver;
            this.costService = costService;
            this.healthStore = healthStore;
            this.quotaTracker = quotaTracker;
            this.routingScoreCalculator = routingScoreCalculator;
            this.logger = logger;
        }

        /// <summary>
        /// Gets a scored and sorted list of provider candidates for the specified model.
        /// </summary>
        /// <param name="modelId">The model identifier.</param>
        /// <param name="streaming">Whether streaming is required.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of enriched candidates sorted by routing score.</returns>
        public async Task<IList<EnrichedCandidate>> GetCandidatesAsync(string modelId, bool streaming, CancellationToken cancellationToken = default)
        {
            var caps = new RequiredCapabilities { Streaming = streaming };
            var resolution = await this.modelResolver.ResolveAsync(modelId, EndpointKind.ChatCompletions, caps).ConfigureAwait(false);

            if (resolution.candidates.Count == 0)
            {
                this.logger.LogWarning("No providers found for model '{ModelId}' with required capabilities.", modelId);
                throw new ArgumentException($"No providers available for model '{modelId}' with the requested capabilities.", nameof(modelId));
            }

            var enriched = new List<EnrichedCandidate>();
            foreach (var candidate in resolution.candidates)
            {
                if (!await this.healthStore.IsHealthyAsync(candidate.Key!, cancellationToken).ConfigureAwait(false))
                {
                    this.logger.LogDebug("Skipping unhealthy provider '{ProviderKey}'", candidate.Key);
                    continue;
                }

                if (!await this.quotaTracker.CheckQuotaAsync(candidate.Key!, cancellationToken).ConfigureAwait(false))
                {
                    this.logger.LogDebug("Skipping quota-exceeded provider '{ProviderKey}'", candidate.Key);
                    continue;
                }

                var cost = await this.costService.GetCostAsync(candidate.Key!, resolution.canonicalId.modelPath, cancellationToken).ConfigureAwait(false);
                enriched.Add(new EnrichedCandidate(candidate, cost, resolution.canonicalId.modelPath));
            }

            var scoredCandidates = new List<(EnrichedCandidate Candidate, double Score)>();
            foreach (var candidate in enriched)
            {
                var score = this.routingScoreCalculator.CalculateScore(candidate, tenantId: null, userId: null);
                scoredCandidates.Add((candidate, score));
                this.logger.LogDebug("Provider '{ProviderKey}' routing score: {Score}", candidate.Key, score);
            }

            return scoredCandidates
                .OrderBy(x => x.Candidate.IsFree ? 0 : 1) // Primary sort: free providers first
                .ThenByDescending(x => x.Score) // Secondary sort: higher score first (incorporates cost)
                .ThenBy(x => x.Candidate.config.Tier) // Tertiary sort: lower tier as tiebreaker
                .Select(x => x.Candidate)
                .ToList();
        }
    }
}
