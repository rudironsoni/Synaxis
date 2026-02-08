// <copyright file="TeamAdminRequirement.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Authorization;

using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Represents the requirement that a user must be a Team Administrator for a specific team.
/// </summary>
public class TeamAdminRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TeamAdminRequirement"/> class.
    /// </summary>
    /// <param name="teamId">The ID of the team the user must be an admin of.</param>
    public TeamAdminRequirement(Guid teamId)
    {
        this.TeamId = teamId;
    }

    /// <summary>
    /// Gets the ID of the team the user must be an admin of.
    /// </summary>
    public Guid TeamId { get; }
}
