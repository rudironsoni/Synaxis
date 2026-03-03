// <copyright file="SynaxisUserStore.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Identity
{
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Identity;

    /// <summary>
    /// Custom UserStore for Synaxis that extends ASP.NET Core Identity
    /// with organization-specific functionality and soft delete support.
    /// </summary>
    /// <summary>
    /// SynaxisUserStore class.
    /// </summary>
    public class SynaxisUserStore : UserStore<SynaxisUser, Role, SynaxisDbContext, Guid>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SynaxisUserStore"/> class.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="describer">The error describer.</param>
        public SynaxisUserStore(
            SynaxisDbContext context,
            IdentityErrorDescriber? describer = null)
            : base(context, describer)
        {
        }

        /// <summary>
        /// Finds a user by email within a specific organization.
        /// Respects soft delete (excludes deleted users).
        /// </summary>
        /// <param name="email">The email address to search for.</param>
        /// <param name="organizationId">The organization ID to scope the search.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The user if found, otherwise null.</returns>
        public Task<SynaxisUser?> FindByEmailInOrganizationAsync(
            string email,
            Guid organizationId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.ThrowIfDisposed();

            var normalizedEmail = NormalizeEmail(email);

            return this.Users
                .Where(u => u.NormalizedEmail == normalizedEmail && u.DeletedAt == null)
                .Where(u => u.OrganizationMemberships.Any(m =>
                    m.OrganizationId == organizationId &&
                    m.Status == "Active"))
                .FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// Gets all organizations that a user belongs to.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A collection of organizations.</returns>
        public async Task<IList<Organization>> GetOrganizationsAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.ThrowIfDisposed();

            return await this.Context.UserOrganizationMemberships
                .Where(m => m.UserId == userId && m.Status == "Active")
                .Include(m => m.Organization)
                .Where(m => m.Organization.DeletedAt == null)
                .Select(m => m.Organization)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Finds a user by ID, respecting soft delete.
        /// </summary>
        /// <param name="userId">The user ID to search for.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The user if found, otherwise null.</returns>
        public override async Task<SynaxisUser?> FindByIdAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.ThrowIfDisposed();

            if (!Guid.TryParse(userId, out var id))
            {
                return null;
            }

            return await this.Users
                .Where(u => u.Id == id && u.DeletedAt == null)
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Finds a user by email, respecting soft delete.
        /// </summary>
        /// <param name="normalizedEmail">The normalized email address to search for.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The user if found, otherwise null.</returns>
        public override Task<SynaxisUser?> FindByEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.ThrowIfDisposed();

            return this.Users
                .Where(u => u.NormalizedEmail == normalizedEmail && u.DeletedAt == null)
                .FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// Finds a user by username, respecting soft delete.
        /// </summary>
        /// <param name="normalizedUserName">The normalized username to search for.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The user if found, otherwise null.</returns>
        public override Task<SynaxisUser?> FindByNameAsync(
            string normalizedUserName,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.ThrowIfDisposed();

            return this.Users
                .Where(u => u.NormalizedUserName == normalizedUserName && u.DeletedAt == null)
                .FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// Gets users in a specific role, respecting soft delete.
        /// </summary>
        /// <param name="normalizedRoleName">The normalized role name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of users in the specified role.</returns>
        public override async Task<IList<SynaxisUser>> GetUsersInRoleAsync(
            string normalizedRoleName,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            this.ThrowIfDisposed();

            var role = await this.Context.Roles
                .Where(r => r.NormalizedName == normalizedRoleName)
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            if (role == null)
            {
                return new List<SynaxisUser>();
            }

            return await (from user in this.Users
                          join userRole in this.Context.UserRoles on user.Id equals userRole.UserId
                          where userRole.RoleId == role.Id && user.DeletedAt == null
                          select user)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Normalizes an email address.
        /// </summary>
        /// <param name="email">The email address to normalize.</param>
        /// <returns>The normalized email address.</returns>
        private static string? NormalizeEmail(string? email)
        {
            return email?.ToUpperInvariant();
        }
    }
}
