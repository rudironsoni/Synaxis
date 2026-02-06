// <copyright file="TenantModelLimit.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    /// <summary>
    /// Represents model-specific limits for a tenant.
    /// </summary>
    public sealed class TenantModelLimit
    {
        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the tenant ID.
        /// </summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the global model ID (FK to GlobalModel).
        /// </summary>
        public string GlobalModelId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the allowed requests per minute.
        /// </summary>
        public int? AllowedRPM { get; set; }

        /// <summary>
        /// Gets or sets the monthly budget.
        /// </summary>
        public decimal? MonthlyBudget { get; set; }

        /// <summary>
        /// Gets or sets the global model navigation property.
        /// </summary>
        [ForeignKey("GlobalModelId")]
        public GlobalModel? GlobalModel { get; set; }
    }
}
