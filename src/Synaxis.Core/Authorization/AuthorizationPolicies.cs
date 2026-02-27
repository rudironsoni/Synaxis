// <copyright file="AuthorizationPolicies.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Authorization;

/// <summary>
/// Defines authorization policy names and role constants for the RBAC system.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>
    /// Policy name for Organization Administrators with full organization management rights.
    /// </summary>
    public const string OrgAdminPolicy = "OrgAdmin";

    /// <summary>
    /// Policy name for Team Administrators with team-level management rights.
    /// </summary>
    public const string TeamAdminPolicy = "TeamAdmin";

    /// <summary>
    /// Policy name for Members with read/write access within their teams.
    /// </summary>
    public const string MemberPolicy = "Member";

    /// <summary>
    /// Policy name for Viewers with read-only access.
    /// </summary>
    public const string ViewerPolicy = "Viewer";

    /// <summary>
    /// Nested class containing role name constants for use in claims and authorization.
    /// </summary>
    public static class Roles
    {
        /// <summary>
        /// Role name for Organization Administrators.
        /// </summary>
        public const string OrgAdmin = "OrgAdmin";

        /// <summary>
        /// Role name for Team Administrators.
        /// </summary>
        public const string TeamAdmin = "TeamAdmin";

        /// <summary>
        /// Role name for standard Members.
        /// </summary>
        public const string Member = "Member";

        /// <summary>
        /// Role name for Viewers with read-only access.
        /// </summary>
        public const string Viewer = "Viewer";
    }
}
