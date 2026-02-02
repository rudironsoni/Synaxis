namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities;

public sealed class User
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public UserRole Role { get; set; }
    public string AuthProvider { get; set; } = string.Empty;
    public string ProviderUserId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Tenant? Tenant { get; set; }
    public ICollection<Project> Projects { get; } = new List<Project>();
}
