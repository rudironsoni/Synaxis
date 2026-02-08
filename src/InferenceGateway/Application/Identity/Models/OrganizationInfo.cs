// <copyright file="OrganizationInfo.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Identity.Models
{
    using System;

    /// <summary>
    /// Organization information response model.
    /// </summary>
    public class OrganizationInfo
    {
        /// <summary>
        /// Gets or sets the organization ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the organization display name.
        /// </summary>
        public required string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the organization slug.
        /// </summary>
        public required string Slug { get; set; }

        /// <summary>
        /// Gets or sets the user's role in this organization.
        /// </summary>
        public string? Role { get; set; }
    }
}
