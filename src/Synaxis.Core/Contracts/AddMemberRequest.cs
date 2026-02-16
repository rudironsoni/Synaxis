// <copyright file="AddMemberRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Contracts
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Request to add a member to a team.
    /// </summary>
    public class AddMemberRequest
    {
        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the role.
        /// </summary>
        public required string Role { get; set; }
    }

    /// <summary>
    /// Request to update a member's role.
    /// </summary>
    public class UpdateMemberRoleRequest
    {
        /// <summary>
        /// Gets or sets the new role.
        /// </summary>
        public required string Role { get; set; }
    }

    /// <summary>
    /// Team member response DTO.
    /// </summary>
    public class TeamMemberResponse
    {
        /// <summary>
        /// Gets or sets the membership ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the user ID.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the user email.
        /// </summary>
        public required string UserEmail { get; set; }

        /// <summary>
        /// Gets or sets the user full name.
        /// </summary>
        public string UserFullName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the team ID.
        /// </summary>
        public Guid TeamId { get; set; }

        /// <summary>
        /// Gets or sets the role.
        /// </summary>
        public required string Role { get; set; }

        /// <summary>
        /// Gets or sets the join date.
        /// </summary>
        public DateTime JoinedAt { get; set; }

        /// <summary>
        /// Gets or sets the inviter ID.
        /// </summary>
        public Guid? InvitedBy { get; set; }
    }

    /// <summary>
    /// Paginated list of team members.
    /// </summary>
    public class TeamMemberListResponse
    {
        /// <summary>
        /// Gets or sets the list of members.
        /// </summary>
        public required IList<TeamMemberResponse> Members { get; set; }

        /// <summary>
        /// Gets or sets the total count.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Gets or sets the page number.
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Gets or sets the page size.
        /// </summary>
        public int PageSize { get; set; }
    }

    /// <summary>
    /// User's team response DTO.
    /// </summary>
    public class UserTeamResponse
    {
        /// <summary>
        /// Gets or sets the team ID.
        /// </summary>
        public Guid TeamId { get; set; }

        /// <summary>
        /// Gets or sets the team name.
        /// </summary>
        public required string TeamName { get; set; }

        /// <summary>
        /// Gets or sets the organization ID.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the organization name.
        /// </summary>
        public required string OrganizationName { get; set; }

        /// <summary>
        /// Gets or sets the user's role in the team.
        /// </summary>
        public required string Role { get; set; }

        /// <summary>
        /// Gets or sets the join date.
        /// </summary>
        public DateTime JoinedAt { get; set; }
    }
}
