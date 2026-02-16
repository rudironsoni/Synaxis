// <copyright file="ViewerAuthorizationHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Authorization;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Authorization handler that evaluates whether a user meets the Viewer requirement.
/// </summary>
public class ViewerAuthorizationHandler : AuthorizationHandler<ViewerRequirement>
{
    /// <inheritdoc/>
    protected override Task HandleRequirementAsync(
    AuthorizationHandlerContext context,
    ViewerRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        if (HasAnyRoleClaim(context.User))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private static bool HasAnyRoleClaim(ClaimsPrincipal user)
    {
        return user.HasClaim(c => c.Type.Equals(ClaimTypes.Role, StringComparison.Ordinal));
    }
}
