using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Identity;

namespace Synaxis.InferenceGateway.Infrastructure.Identity;

/// <summary>
/// Custom RoleStore for Synaxis that extends ASP.NET Core Identity
/// with support for system roles and organization-specific roles.
/// </summary>
public class SynaxisRoleStore : RoleStore<Role, SynaxisDbContext, Guid>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SynaxisRoleStore"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="describer">The error describer.</param>
    public SynaxisRoleStore(
        SynaxisDbContext context,
        IdentityErrorDescriber? describer = null)
        : base(context, describer)
    {
    }

    /// <summary>
    /// Gets all roles for a specific organization, including system roles.
    /// System roles have OrganizationId = null.
    /// </summary>
    /// <param name="organizationId">The organization ID to filter by. Null returns only system roles.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of roles.</returns>
    public async Task<IList<Role>> GetRolesByOrganizationAsync(
        Guid? organizationId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        if (organizationId == null)
        {
            // Return only system roles
            return await Roles
                .Where(r => r.IsSystemRole && r.OrganizationId == null)
                .ToListAsync(cancellationToken);
        }

        // Return organization-specific roles and system roles
        return await Roles
            .Where(r => r.IsSystemRole || r.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets all system roles (OrganizationId = null, IsSystemRole = true).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of system roles.</returns>
    public async Task<IList<Role>> GetSystemRolesAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        return await Roles
            .Where(r => r.IsSystemRole && r.OrganizationId == null)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets all organization-specific roles for a given organization.
    /// </summary>
    /// <param name="organizationId">The organization ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of organization-specific roles.</returns>
    public async Task<IList<Role>> GetOrganizationSpecificRolesAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        return await Roles
            .Where(r => !r.IsSystemRole && r.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Finds a role by name within a specific organization context.
    /// Searches both system roles and organization-specific roles.
    /// </summary>
    /// <param name="normalizedRoleName">The normalized role name.</param>
    /// <param name="organizationId">The organization ID. Null searches only system roles.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The role if found, otherwise null.</returns>
    public async Task<Role?> FindByNameInOrganizationAsync(
        string normalizedRoleName,
        Guid? organizationId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        if (organizationId == null)
        {
            // Search only system roles
            return await Roles
                .Where(r => r.NormalizedName == normalizedRoleName && 
                           r.IsSystemRole && 
                           r.OrganizationId == null)
                .FirstOrDefaultAsync(cancellationToken);
        }

        // Search organization-specific roles first, then fall back to system roles
        var orgRole = await Roles
            .Where(r => r.NormalizedName == normalizedRoleName && 
                       r.OrganizationId == organizationId)
            .FirstOrDefaultAsync(cancellationToken);

        if (orgRole != null)
        {
            return orgRole;
        }

        // Fall back to system roles
        return await Roles
            .Where(r => r.NormalizedName == normalizedRoleName && 
                       r.IsSystemRole && 
                       r.OrganizationId == null)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
