// <copyright file="MemberRequirement.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Authorization;

using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Represents the requirement that a user must be a Member (or higher role) of a specific team.
/// </summary>
public class MemberRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MemberRequirement"/> class.
    /// </summary>
    /// <param name="teamId">The ID of the team the user must be a member of.</param>
    public MemberRequirement(Guid teamId)
    {
        this.TeamId = teamId;
    }

    /// <summary>
    /// Gets the ID of the team the user must be a member of.
    /// </summary>
    public Guid TeamId { get; }
}
