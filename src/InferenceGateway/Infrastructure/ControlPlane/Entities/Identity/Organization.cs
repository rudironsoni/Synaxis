namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Identity;

/// <summary>
/// Represents an organization (tenant) in the identity schema.
/// </summary>
public class Organization
{
    public Guid Id { get; set; }
    public required string LegalName { get; set; }
    public required string DisplayName { get; set; }
    public required string Slug { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? TaxId { get; set; }
    public string? LegalAddress { get; set; }
    public string? PrimaryContactEmail { get; set; }
    public string? BillingEmail { get; set; }
    public string? SupportEmail { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Industry { get; set; }
    public string? CompanySize { get; set; }
    public string? WebsiteUrl { get; set; }
    public required string Status { get; set; } = "Active";
    public required string PlanTier { get; set; } = "Free";
    public DateTime? TrialEndsAt { get; set; }
    public bool RequireMfa { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    // Navigation properties
    public OrganizationSettings? Settings { get; set; }
    public ICollection<Group> Groups { get; set; } = new List<Group>();
    public ICollection<UserOrganizationMembership> UserMemberships { get; set; } = new List<UserOrganizationMembership>();
    public ICollection<Role> Roles { get; set; } = new List<Role>();
}
