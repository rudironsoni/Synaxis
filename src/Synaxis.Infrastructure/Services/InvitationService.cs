// <copyright file="InvitationService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.Services
{
#nullable enable

    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Synaxis.Core.Contracts;
    using Synaxis.Core.Models;
    using Synaxis.Infrastructure.Data;

    /// <summary>
    /// Service for managing team invitations.
    /// </summary>
    public class InvitationService : IInvitationService
    {
        private const int TokenBytesLength = 32;
        private const int InvitationExpirationDays = 7;
        private readonly SynaxisDbContext _context;
        private readonly ITeamMembershipService _teamMembershipService;

        /// <summary>
        /// Initializes a new instance of the <see cref="InvitationService"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="teamMembershipService">The team membership service.</param>
        public InvitationService(SynaxisDbContext context, ITeamMembershipService teamMembershipService)
        {
            this._context = context;
            this._teamMembershipService = teamMembershipService;
        }

        /// <inheritdoc/>
        public async Task<InvitationResponse> CreateInvitationAsync(
            Guid teamId,
            string email,
            string role,
            Guid invitedByUserId,
            CancellationToken cancellationToken = default)
        {
            ValidateEmail(email);
            var team = await this.GetTeamWithOrganizationAsync(teamId, cancellationToken).ConfigureAwait(false);
            await this.ValidateUserNotMemberAsync(email, teamId, cancellationToken).ConfigureAwait(false);
            await this.ValidateNoPendingInvitationAsync(email, teamId, cancellationToken).ConfigureAwait(false);

            var invitation = this.CreateInvitation(team, teamId, email, role, invitedByUserId);
            this._context.Set<Invitation>().Add(invitation);
            await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            invitation.Team = team;
            invitation.Team.Organization = team.Organization;

            return MapToResponse(invitation);
        }

        private static void ValidateEmail(string email)
        {
            if (!IsValidEmail(email))
            {
                throw new ArgumentException("The provided email address is invalid.", nameof(email));
            }
        }

        private async Task<Team> GetTeamWithOrganizationAsync(Guid teamId, CancellationToken cancellationToken)
        {
            var team = await this._context.Set<Team>()
                .Include(t => t.Organization)
                .FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken)
                .ConfigureAwait(false);

            if (team == null)
            {
                throw new InvalidOperationException("Team not found.");
            }

            return team;
        }

        private async Task ValidateUserNotMemberAsync(string email, Guid teamId, CancellationToken cancellationToken)
        {
            var user = await this._context.Set<User>()
                .FirstOrDefaultAsync(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase), cancellationToken)
                .ConfigureAwait(false);

            if (user != null)
            {
                var existingMembership = await this._context.Set<TeamMembership>()
                    .AnyAsync(tm => tm.UserId == user.Id && tm.TeamId == teamId, cancellationToken)
                    .ConfigureAwait(false);

                if (existingMembership)
                {
                    throw new InvalidOperationException("User is already a member of this team.");
                }
            }
        }

        private async Task ValidateNoPendingInvitationAsync(string email, Guid teamId, CancellationToken cancellationToken)
        {
            var existingPendingInvitation = await this._context.Set<Invitation>()
                .AnyAsync(
                    i => string.Equals(i.Email, email, StringComparison.OrdinalIgnoreCase)
                         && i.TeamId == teamId
                         && i.Status == "pending"
                         && i.ExpiresAt > DateTime.UtcNow,
                    cancellationToken)
                .ConfigureAwait(false);

            if (existingPendingInvitation)
            {
                throw new InvalidOperationException("A pending invitation already exists for this email address.");
            }
        }

        private Invitation CreateInvitation(Team team, Guid teamId, string email, string role, Guid invitedByUserId)
        {
            var token = GenerateSecureToken();

            return new Invitation
            {
                Id = Guid.NewGuid(),
                OrganizationId = team.OrganizationId,
                TeamId = teamId,
                Email = email,
                Role = role,
                Token = token,
                InvitedBy = invitedByUserId,
                Status = "pending",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(InvitationExpirationDays),
            };
        }

        /// <inheritdoc/>
        public async Task<InvitationResponse> GetInvitationAsync(string token, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("Token is required", nameof(token));
            }

            var invitation = await this._context.Set<Invitation>()
                .Include(i => i.Team)
                    .ThenInclude(t => t!.Organization)
                .FirstOrDefaultAsync(i => i.Token == token, cancellationToken)
                .ConfigureAwait(false);

            if (invitation == null)
            {
                throw new InvalidOperationException("Invitation not found.");
            }

            return MapToResponse(invitation);
        }

        /// <inheritdoc/>
        public async Task AcceptInvitationAsync(string token, Guid userId, CancellationToken cancellationToken = default)
        {
            var invitation = await this._context.Set<Invitation>()
                .Include(i => i.Team)
                .FirstOrDefaultAsync(i => i.Token == token, cancellationToken)
                .ConfigureAwait(false);

            if (invitation == null)
            {
                throw new InvalidOperationException("Invitation not found.");
            }

            if (invitation.IsExpired)
            {
                throw new InvalidOperationException("This invitation has expired.");
            }

            if (!string.Equals(invitation.Status, "pending", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("This invitation has already been processed.");
            }

            // Verify user email matches invitation
            var user = await this._context.Set<User>()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                .ConfigureAwait(false);

            if (user == null)
            {
                throw new InvalidOperationException("User not found.");
            }

            if (!string.Equals(user.Email, invitation.Email, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The invitation email address does not match your email address.");
            }

            // Update invitation status
            invitation.Status = "accepted";
            invitation.AcceptedAt = DateTime.UtcNow;
            invitation.AcceptedBy = userId;

            // Add user to team
            await this._teamMembershipService.AddMemberAsync(
                invitation.TeamId,
                userId,
                invitation.Role,
                invitation.InvitedBy,
                cancellationToken).ConfigureAwait(false);

            await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task DeclineInvitationAsync(string token, Guid userId, CancellationToken cancellationToken = default)
        {
            var invitation = await this._context.Set<Invitation>()
                .Include(i => i.Team)
                .FirstOrDefaultAsync(i => i.Token == token, cancellationToken)
                .ConfigureAwait(false);

            if (invitation == null)
            {
                throw new InvalidOperationException("Invitation not found.");
            }

            if (invitation.IsExpired)
            {
                throw new InvalidOperationException("This invitation has expired.");
            }

            if (!string.Equals(invitation.Status, "pending", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("This invitation has already been processed.");
            }

            // Verify user email matches invitation
            var user = await this._context.Set<User>()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
                .ConfigureAwait(false);

            if (user == null)
            {
                throw new InvalidOperationException("User not found.");
            }

            if (!string.Equals(user.Email, invitation.Email, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The invitation email address does not match your email address.");
            }

            // Update invitation status
            invitation.Status = "declined";
            invitation.DeclinedAt = DateTime.UtcNow;
            invitation.DeclinedBy = userId;

            await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<InvitationListResponse> ListPendingInvitationsAsync(
            Guid organizationId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = this._context.Set<Invitation>()
                .Include(i => i.Team)
                    .ThenInclude(t => t!.Organization)
                .Where(i => i.OrganizationId == organizationId && i.Status == "pending")
                .OrderByDescending(i => i.CreatedAt)
                .AsQueryable();

            var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
            var invitations = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return new InvitationListResponse
            {
                Invitations = invitations.Select(MapToResponse).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
            };
        }

        /// <inheritdoc/>
        public async Task CancelInvitationAsync(Guid invitationId, Guid cancelledByUserId, CancellationToken cancellationToken = default)
        {
            var invitation = await this._context.Set<Invitation>()
                .Include(i => i.Team)
                .FirstOrDefaultAsync(i => i.Id == invitationId, cancellationToken)
                .ConfigureAwait(false);

            if (invitation == null)
            {
                throw new InvalidOperationException("Invitation not found.");
            }

            if (!string.Equals(invitation.Status, "pending", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("This invitation has already been processed.");
            }

            // Update invitation status
            invitation.Status = "cancelled";
            invitation.CancelledAt = DateTime.UtcNow;
            invitation.CancelledBy = cancelledByUserId;

            await this._context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        private static string GenerateSecureToken()
        {
            var bytes = new byte[TokenBytesLength];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            return Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            return new EmailAddressAttribute().IsValid(email);
        }

        private static InvitationResponse MapToResponse(Invitation invitation)
        {
            return new InvitationResponse
            {
                Id = invitation.Id,
                TeamId = invitation.TeamId,
                TeamName = invitation.Team?.Name ?? string.Empty,
                OrganizationId = invitation.OrganizationId,
                OrganizationName = invitation.Team?.Organization?.Name ?? string.Empty,
                Email = invitation.Email,
                Role = invitation.Role,
                Status = invitation.Status,
                Token = invitation.Token,
                InvitedBy = invitation.InvitedBy,
                CreatedAt = invitation.CreatedAt,
                ExpiresAt = invitation.ExpiresAt,
                IsExpired = invitation.IsExpired,
            };
        }
    }
}
