// <copyright file="UserInfo.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Identity.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// User information response model.
    /// </summary>
    public class UserInfo
    {
        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the user's email.
        /// </summary>
        required public string Email { get; set; }

        /// <summary>
        /// Gets or sets the user's first name.
        /// </summary>
        public string? FirstName { get; set; }

        /// <summary>
        /// Gets or sets the user's last name.
        /// </summary>
        public string? LastName { get; set; }

        /// <summary>
        /// Gets or sets the user's current organization.
        /// </summary>
        public OrganizationInfo? CurrentOrganization { get; set; }

        /// <summary>
        /// Gets or sets the user's organizations.
        /// </summary>
        public IList<OrganizationInfo> Organizations { get; set; } = new List<OrganizationInfo>();
    }
}
