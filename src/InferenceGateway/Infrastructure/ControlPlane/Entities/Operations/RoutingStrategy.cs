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
        public Guid Id { get; set; }

        public Guid OrganizationId { get; set; }

        public required string Name { get; set; }

        public string? Description { get; set; }

        public required string StrategyType { get; set; } = "CostOptimized";

        public bool PrioritizeFreeProviders { get; set; } = true;

        public decimal? MaxCostPer1MTokens { get; set; }

        public bool FallbackToPaid { get; set; } = true;

        public int? MaxLatencyMs { get; set; }

        public bool RequireStreaming { get; set; }

        public decimal? MinHealthScore { get; set; }

        public bool IsDefault { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties - will be configured with cross-schema relationships
    }
}
