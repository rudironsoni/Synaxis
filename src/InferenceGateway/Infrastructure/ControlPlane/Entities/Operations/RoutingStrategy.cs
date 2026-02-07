// <copyright file="RoutingStrategy.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Operations
{
    /// <summary>
    /// Represents a routing strategy for model selection.
    /// </summary>
    public class RoutingStrategy
    {
        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the OrganizationId.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the name of the routing strategy.
        /// </summary>
        required public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the routing strategy.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the type of the routing strategy (e.g., "CostOptimized", "Performance", "Balanced").
        /// </summary>
        required public string StrategyType { get; set; } = "CostOptimized";

        /// <summary>
        /// Gets or sets a value indicating whether free providers should be prioritized over paid providers.
        /// </summary>
        public bool PrioritizeFreeProviders { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum cost per 1 million tokens allowed for this strategy.
        /// </summary>
        public decimal? MaxCostPer1MTokens { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to fallback to paid providers when free providers are unavailable.
        /// </summary>
        public bool FallbackToPaid { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum latency in milliseconds allowed for provider selection.
        /// </summary>
        public int? MaxLatencyMs { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether streaming support is required.
        /// </summary>
        public bool RequireStreaming { get; set; }

        /// <summary>
        /// Gets or sets the minimum health score required for a provider to be considered.
        /// </summary>
        public decimal? MinHealthScore { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is the default routing strategy for the organization.
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this routing strategy is active and can be used.
        /// </summary>
        public bool IsActive { get; set; } = true;

        // Navigation properties - will be configured with cross-schema relationships
    }
}
