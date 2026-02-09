// <copyright file="InvitationService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

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

namespace Synaxis.Infrastructure.Services
{
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
            // Validate email format
            if (!IsValidEmail(email))
            {
                throw new ArgumentException("The provided email address is invalid.", nameof(email));
            }

            // Load team with organization
            var team = await this._context.Set<Team>()
                .Include(t => t.Organization)
                .FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);

            if (team == null)
            {
                throw new InvalidOperationException("Team not found.");
            }

            // Check if user is already a member
            var user = await this._context.Set<User>()
                .FirstOrDefaultAsync(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase), cancellationToken);

            if (user != null)
            {
                var existingMembership = await this._context.Set<TeamMembership>()
                    .AnyAsync(tm => tm.UserId == user.Id && tm.TeamId == teamId, cancellationToken);

                if (existingMembership)
                {
                    throw new InvalidOperationException("User is already a member of this team.");
                }
            }

            // Check for duplicate pending invitation
            var existingPendingInvitation = await this._context.Set<Invitation>()
                .AnyAsync(
                    i => string.Equals(i.Email, email, StringComparison.OrdinalIgnoreCase)
                         && i.TeamId == teamId
                         && i.Status == "pending"
                         && i.ExpiresAt > DateTime.UtcNow,
                    cancellationToken);

            if (existingPendingInvitation)
            {
                throw new InvalidOperationException("A pending invitation already exists for this email address.");
            }

            // Generate secure token
            var token = GenerateSecureToken();

            // Create invitation
            var invitation = new Invitation
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

            this._context.Set<Invitation>().Add(invitation);
            await this._context.SaveChangesAsync(cancellationToken);

            // Reload with navigation properties for response
            invitation.Team = team;
            invitation.Team.Organization = team.Organization;

            return MapToResponse(invitation);
        }

        /// <inheritdoc/>
        public async Task<InvitationResponse?> GetInvitationAsync(string token, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            var invitation = await this._context.Set<Invitation>()
                .Include(i => i.Team)
                    .ThenInclude(t => t!.Organization)
                .FirstOrDefaultAsync(i => i.Token == token, cancellationToken);

            return invitation != null ? MapToResponse(invitation) : null;
        }

        /// <inheritdoc/>
        public async Task AcceptInvitationAsync(string token, Guid userId, CancellationToken cancellationToken = default)
        {
            var invitation = await this._context.Set<Invitation>()
                .Include(i => i.Team)
                .FirstOrDefaultAsync(i => i.Token == token, cancellationToken);

            if (invitation == null)
            {
                throw new InvalidOperationException("Invitation not found.");
            }

            if (invitation.IsExpired)
            {
                throw new InvalidOperationException("This invitation has expired.");
            }

            if (invitation.Status != "pending")
            {
                throw new InvalidOperationException("This invitation has already been processed.");
            }

            // Verify user email matches invitation
            var user = await this._context.Set<User>()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

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
                cancellationToken);

            await this._context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task DeclineInvitationAsync(string token, Guid userId, CancellationToken cancellationToken = default)
        {
            var invitation = await this._context.Set<Invitation>()
                .Include(i => i.Team)
                .FirstOrDefaultAsync(i => i.Token == token, cancellationToken);

            if (invitation == null)
            {
                throw new InvalidOperationException("Invitation not found.");
            }

            if (invitation.IsExpired)
            {
                throw new InvalidOperationException("This invitation has expired.");
            }

            if (invitation.Status != "pending")
            {
                throw new InvalidOperationException("This invitation has already been processed.");
            }

            // Verify user email matches invitation
            var user = await this._context.Set<User>()
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

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

            await this._context.SaveChangesAsync(cancellationToken);
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
                .OrderByDescending(i => i.CreatedAt);

            var totalCount = await query.CountAsync(cancellationToken);
            var invitations = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

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
                .FirstOrDefaultAsync(i => i.Id == invitationId, cancellationToken);

            if (invitation == null)
            {
                throw new InvalidOperationException("Invitation not found.");
            }

            if (invitation.Status != "pending")
            {
                throw new InvalidOperationException("This invitation has already been processed.");
            }

            // Update invitation status
            invitation.Status = "cancelled";
            invitation.CancelledAt = DateTime.UtcNow;
            invitation.CancelledBy = cancelledByUserId;

            await this._context.SaveChangesAsync(cancellationToken);
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
