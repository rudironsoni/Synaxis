namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Identity;

/// <summary>
/// Represents a user's membership in an organization.
/// </summary>
public class UserOrganizationMembership
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public required string OrganizationRole { get; set; } = "Member";
    public Guid? PrimaryGroupId { get; set; }
    public int? RateLimitRpm { get; set; }
    public int? RateLimitTpm { get; set; }
    public bool AllowAutoOptimization { get; set; } = true;
    public required string Status { get; set; } = "Active";

    // Navigation properties
    public SynaxisUser User { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
    public Group? PrimaryGroup { get; set; }
}
