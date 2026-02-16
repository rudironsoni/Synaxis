// <copyright file="MemberAuthorizationHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Authorization;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Authorization handler that evaluates whether a user meets the Member requirement.
/// </summary>
public class MemberAuthorizationHandler : AuthorizationHandler<MemberRequirement>
{
    /// <summary>
    /// The claim type used for team identification.
    /// </summary>
    internal const string TeamIdClaimType = "team_id";

    /// <inheritdoc/>
    protected override Task HandleRequirementAsync(
    AuthorizationHandlerContext context,
    MemberRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        if (!IsMemberOfTeam(context.User, requirement.TeamId))
        {
            return Task.CompletedTask;
        }

        if (IsMemberOrHigher(context.User))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private static bool IsMemberOfTeam(ClaimsPrincipal user, Guid teamId)
    {
        var teamClaim = user.Claims.FirstOrDefault(c => c.Type.Equals(TeamIdClaimType, StringComparison.Ordinal));
        if (teamClaim == null)
        {
            return false;
        }

        return Guid.TryParse(teamClaim.Value, out var userTeamId) && userTeamId == teamId;
    }

    private static bool IsMemberOrHigher(ClaimsPrincipal user)
    {
        return user.IsInRole(AuthorizationPolicies.Roles.Member) ||
        user.IsInRole(AuthorizationPolicies.Roles.TeamAdmin) ||
        user.IsInRole(AuthorizationPolicies.Roles.OrgAdmin);
    }
}
