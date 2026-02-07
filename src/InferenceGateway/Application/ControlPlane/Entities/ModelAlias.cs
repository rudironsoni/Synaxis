// <copyright file="ModelAlias.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities
{
    using System;

    /// <summary>
    /// Represents an alias mapping for a model.
    /// </summary>
    public sealed class ModelAlias
    {
        /// <summary>
        /// Gets or sets the model alias ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the tenant ID.
        /// </summary>
        public Guid TenantId { get; set; }

        /// <summary>
        /// Gets or sets the alias name.
        /// </summary>
        public string Alias { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the target model.
        /// </summary>
        public string TargetModel { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the tenant navigation property.
        /// </summary>
        public Tenant? Tenant { get; set; }
    }
}
