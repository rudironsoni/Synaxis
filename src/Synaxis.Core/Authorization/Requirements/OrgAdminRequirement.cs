// <copyright file="OrgAdminRequirement.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Authorization;

using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Represents the requirement that a user must be an Organization Administrator.
/// </summary>
public class OrgAdminRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrgAdminRequirement"/> class.
    /// </summary>
    public OrgAdminRequirement()
    {
    }
}
