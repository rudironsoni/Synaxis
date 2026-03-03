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
        /// <summary>
        /// Gets or sets the unique identifier for this organization model configuration.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the organization that owns this model configuration.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the model being configured.
        /// </summary>
        public Guid ModelId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this model is enabled for the organization.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the custom display name for this model within the organization.
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the cost per 1 million input tokens, or null to use default pricing.
        /// </summary>
        public decimal? InputCostPer1MTokens { get; set; }

        /// <summary>
        /// Gets or sets the cost per 1 million output tokens, or null to use default pricing.
        /// </summary>
        public decimal? OutputCostPer1MTokens { get; set; }

        /// <summary>
        /// Gets or sets a custom alias for this model within the organization.
        /// </summary>
        public string? CustomAlias { get; set; }

        // Navigation properties - will be configured with cross-schema relationships
    }
}
