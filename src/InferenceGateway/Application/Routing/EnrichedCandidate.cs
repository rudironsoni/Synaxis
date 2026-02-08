// <copyright file="EnrichedCandidate.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Routing
{
    using Synaxis.InferenceGateway.Application.Configuration;
    using Synaxis.InferenceGateway.Application.ControlPlane.Entities;

    /// <summary>
    /// Represents a provider candidate enriched with cost information and model path for routing decisions.
    /// </summary>
    /// <param name="Config">The provider configuration details.</param>
    /// <param name="Cost">Optional cost information for the model.</param>
    /// <param name="CanonicalModelPath">The canonical path identifier for the model.</param>
    public record EnrichedCandidate(ProviderConfig Config, ModelCost? Cost, string CanonicalModelPath)
    {
        /// <summary>
        /// Gets the unique identifier key for this provider configuration.
        /// </summary>
        public string Key => this.Config.Key!;

        /// <summary>
        /// Gets a value indicating whether this candidate is a free tier provider.
        /// Checks both config flag (static free providers) and cost database (dynamic free tier).
        /// </summary>
        public bool IsFree => this.Config.IsFree || (this.Cost?.FreeTier ?? false);

        /// <summary>
        /// Gets the cost per token for this provider, or MaxValue if no cost information is available.
        /// </summary>
        public decimal CostPerToken => this.Cost?.CostPerToken ?? decimal.MaxValue;
    }
}
