// <copyright file="CreateInvitationRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Contracts
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Request to create an invitation.
    /// </summary>
    public class CreateInvitationRequest
    {
        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        public required string Email { get; set; }

        /// <summary>
        /// Gets or sets the role to assign.
        /// </summary>
        public required string Role { get; set; }
    }

    /// <summary>
    /// Invitation response DTO.
    /// </summary>
    public class InvitationResponse
    {
        /// <summary>
        /// Gets or sets the invitation ID.
        /// </summary>
        public Guid Id { get; set; }

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
        /// Gets or sets the email address.
        /// </summary>
        public required string Email { get; set; }

        /// <summary>
        /// Gets or sets the role.
        /// </summary>
        public required string Role { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        public required string Status { get; set; }

        /// <summary>
        /// Gets or sets the invitation token.
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets the invited by user ID.
        /// </summary>
        public Guid InvitedBy { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the expiration timestamp.
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the invitation is expired.
        /// </summary>
        public bool IsExpired { get; set; }
    }

    /// <summary>
    /// Paginated list of invitations.
    /// </summary>
    public class InvitationListResponse
    {
        /// <summary>
        /// Gets or sets the list of invitations.
        /// </summary>
        public required IList<InvitationResponse> Invitations { get; set; }

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
}
