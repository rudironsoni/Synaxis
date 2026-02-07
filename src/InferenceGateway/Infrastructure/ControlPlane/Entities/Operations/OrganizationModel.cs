// <copyright file="OrganizationModel.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Operations
{
    /// <summary>
    /// Represents an organization's model configuration.
    /// </summary>
    public class OrganizationModel
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid ModelId { get; set; }
        public bool IsEnabled { get; set; } = true;
        public string? DisplayName { get; set; }
        public decimal? InputCostPer1MTokens { get; set; }
        public decimal? OutputCostPer1MTokens { get; set; }
        public string? CustomAlias { get; set; }

        // Navigation properties - will be configured with cross-schema relationships
    }
}
