// <copyright file="ViewerRequirement.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Authorization;

using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Represents the requirement that a user must be authenticated with any role claim for read-only access.
/// </summary>
public class ViewerRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ViewerRequirement"/> class.
    /// </summary>
    public ViewerRequirement()
    {
    }
}
