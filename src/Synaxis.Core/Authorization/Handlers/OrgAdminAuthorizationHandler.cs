// <copyright file="OrgAdminAuthorizationHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Authorization;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Authorization handler that evaluates whether a user meets the Organization Administrator requirement.
/// </summary>
public class OrgAdminAuthorizationHandler : AuthorizationHandler<OrgAdminRequirement>
{
    /// <summary>
    /// The claim type used for organization identification.
    /// </summary>
    internal const string OrganizationIdClaimType = "organization_id";

    /// <inheritdoc/>
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OrgAdminRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        if (!HasOrganizationIdClaim(context.User))
        {
            return Task.CompletedTask;
        }

        if (context.User.IsInRole(AuthorizationPolicies.Roles.OrgAdmin))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private static bool HasOrganizationIdClaim(ClaimsPrincipal user)
    {
        return user.Claims.Any(claim => claim.Type.Equals(OrganizationIdClaimType, StringComparison.Ordinal));
    }
}
