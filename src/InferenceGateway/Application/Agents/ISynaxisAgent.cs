// <copyright file="ISynaxisAgent.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Agents
{
    using System;

    /// <summary>
    /// Base interface for all Synaxis agents with tenant context.
    /// </summary>
    public interface ISynaxisAgent
    {
        /// <summary>
        /// Gets or sets the organization ID.
        /// </summary>
        Guid? OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the group ID.
        /// </summary>
        Guid? GroupId { get; set; }

        /// <summary>
        /// Gets the agent name.
        /// </summary>
        string Name { get; }
    }
}
