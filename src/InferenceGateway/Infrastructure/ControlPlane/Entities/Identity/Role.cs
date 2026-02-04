using Microsoft.AspNetCore.Identity;

namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Identity;

/// <summary>
/// Represents a role in the system, extending ASP.NET Core Identity.
/// Can be system-wide or organization-specific.
/// </summary>
public class Role : IdentityRole<Guid>
{
    public bool IsSystemRole { get; set; }
    public Guid? OrganizationId { get; set; }
    public string? Description { get; set; }

    // Navigation properties
    public Organization? Organization { get; set; }
}
