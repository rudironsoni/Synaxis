// <copyright file="PermissionExtensions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Authorization;

using System.Security.Claims;

/// <summary>
/// Extension methods for permission checks on <see cref="ClaimsPrincipal"/>.
/// </summary>
public static class PermissionExtensions
{
    /// <summary>
    /// The claim type used for organization identification.
    /// </summary>
    internal const string OrganizationIdClaimType = "organization_id";

    /// <summary>
    /// The claim type used for user identification (JWT standard).
    /// </summary>
    internal const string UserIdClaimType = "sub";

    /// <summary>
    /// Determines whether the user is an Organization Administrator.
    /// </summary>
    /// <param name="user">The user principal to check.</param>
    /// <returns><c>true</c> if the user has the OrgAdmin role; otherwise, <c>false</c>.</returns>
    public static bool IsOrgAdmin(this ClaimsPrincipal user)
    {
        if (user == null)
        {
            return false;
        }

        return user.IsInRole(AuthorizationPolicies.Roles.OrgAdmin);
    }

    /// <summary>
    /// Determines whether the user is a Team Administrator for the specified team.
    /// </summary>
    /// <param name="user">The user principal to check.</param>
    /// <param name="teamId">The ID of the team to check membership for.</param>
    /// <returns><c>true</c> if the user is a Team Admin for the team or an OrgAdmin; otherwise, <c>false</c>.</returns>
    public static bool IsTeamAdmin(this ClaimsPrincipal user, Guid teamId)
    {
        if (user == null)
        {
            return false;
        }

        // OrgAdmins can manage any team
        if (user.IsOrgAdmin())
        {
            return true;
        }

        // Check if user has TeamAdmin role and is member of the specified team
        if (!user.IsInRole(AuthorizationPolicies.Roles.TeamAdmin))
        {
            return false;
        }

        return IsMemberOfTeam(user, teamId);
    }

    /// <summary>
    /// Determines whether the user can manage the specified team (add/remove members, change settings).
    /// </summary>
    /// <param name="user">The user principal to check.</param>
    /// <param name="teamId">The ID of the team to check.</param>
    /// <returns><c>true</c> if the user can manage the team; otherwise, <c>false</c>.</returns>
    public static bool CanManageTeam(this ClaimsPrincipal user, Guid teamId)
    {
        return IsTeamAdmin(user, teamId);
    }

    /// <summary>
    /// Determines whether the user can invite members to the specified team.
    /// </summary>
    /// <param name="user">The user principal to check.</param>
    /// <param name="teamId">The ID of the team to check.</param>
    /// <returns><c>true</c> if the user can invite members; otherwise, <c>false</c>.</returns>
    public static bool CanInviteMember(this ClaimsPrincipal user, Guid teamId)
    {
        return IsTeamAdmin(user, teamId);
    }

    /// <summary>
    /// Gets the organization ID from the user's claims.
    /// </summary>
    /// <param name="user">The user principal.</param>
    /// <returns>The organization ID if found and valid; otherwise, <c>null</c>.</returns>
    public static Guid? GetOrganizationId(this ClaimsPrincipal user)
    {
        if (user == null)
        {
            return null;
        }

        var orgClaim = user.FindFirst(OrganizationIdClaimType);
        if (orgClaim == null)
        {
            return null;
        }

        if (Guid.TryParse(orgClaim.Value, out var orgId))
        {
            return orgId;
        }

        return null;
    }

    /// <summary>
    /// Gets the current user ID from the user's claims.
    /// </summary>
    /// <param name="user">The user principal.</param>
    /// <returns>The user ID if found and valid; otherwise, <c>null</c>.</returns>
    public static Guid? GetCurrentUserId(this ClaimsPrincipal user)
    {
        if (user == null)
        {
            return null;
        }

        // Try "sub" claim first (JWT standard)
        var userClaim = user.FindFirst(UserIdClaimType);
        if (userClaim == null)
        {
            // Fall back to NameIdentifier (ASP.NET Core Identity)
            userClaim = user.FindFirst(ClaimTypes.NameIdentifier);
        }

        if (userClaim == null)
        {
            return null;
        }

        if (Guid.TryParse(userClaim.Value, out var userId))
        {
            return userId;
        }

        return null;
    }

    private static bool IsMemberOfTeam(ClaimsPrincipal user, Guid teamId)
    {
        var teamClaim = user.Claims.FirstOrDefault(c => c.Type.Equals(TeamAdminAuthorizationHandler.TeamIdClaimType, StringComparison.Ordinal));
        if (teamClaim == null)
        {
            return false;
        }

        return Guid.TryParse(teamClaim.Value, out var userTeamId) && userTeamId == teamId;
    }
}
