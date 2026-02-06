using Microsoft.AspNetCore.Identity;

namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Identity;

/// <summary>
/// Represents a user in the system, extending ASP.NET Core Identity.
/// </summary>
public class SynaxisUser : IdentityUser<Guid>
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public required string Status { get; set; } = "Active";
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<UserOrganizationMembership> OrganizationMemberships { get; set; } = new List<UserOrganizationMembership>();
    public ICollection<UserGroupMembership> GroupMemberships { get; set; } = new List<UserGroupMembership>();
}
