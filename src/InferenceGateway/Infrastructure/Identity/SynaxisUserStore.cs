using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Identity;

namespace Synaxis.InferenceGateway.Infrastructure.Identity;

/// <summary>
/// Custom UserStore for Synaxis that extends ASP.NET Core Identity
/// with organization-specific functionality and soft delete support.
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
    public async Task<SynaxisUser?> FindByEmailInOrganizationAsync(
        string email,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        var normalizedEmail = NormalizeEmail(email);

        return await Users
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
        ThrowIfDisposed();

        return await Context.UserOrganizationMemberships
            .Where(m => m.UserId == userId && m.Status == "Active")
            .Include(m => m.Organization)
            .Where(m => m.Organization.DeletedAt == null)
            .Select(m => m.Organization)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Finds a user by ID, respecting soft delete.
    /// </summary>
    public override async Task<SynaxisUser?> FindByIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        if (!Guid.TryParse(userId, out var id))
        {
            return null;
        }

        return await Users
            .Where(u => u.Id == id && u.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Finds a user by email, respecting soft delete.
    /// </summary>
    public override async Task<SynaxisUser?> FindByEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        return await Users
            .Where(u => u.NormalizedEmail == normalizedEmail && u.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Finds a user by username, respecting soft delete.
    /// </summary>
    public override async Task<SynaxisUser?> FindByNameAsync(
        string normalizedUserName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        return await Users
            .Where(u => u.NormalizedUserName == normalizedUserName && u.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Gets users in a specific role, respecting soft delete.
    /// </summary>
    public override async Task<IList<SynaxisUser>> GetUsersInRoleAsync(
        string normalizedRoleName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        var role = await Context.Roles
            .Where(r => r.NormalizedName == normalizedRoleName)
            .FirstOrDefaultAsync(cancellationToken);

        if (role == null)
        {
            return new List<SynaxisUser>();
        }

        return await (from user in Users
                      join userRole in Context.UserRoles on user.Id equals userRole.UserId
                      where userRole.RoleId == role.Id && user.DeletedAt == null
                      select user)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Normalizes an email address.
    /// </summary>
    private string? NormalizeEmail(string? email)
    {
        return email?.ToUpperInvariant();
    }
}
