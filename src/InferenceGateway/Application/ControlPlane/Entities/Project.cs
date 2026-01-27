namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities;

public sealed class Project
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ProjectStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Tenant? Tenant { get; set; }
    public ICollection<ApiKey> ApiKeys { get; } = new List<ApiKey>();
    public ICollection<User> Users { get; } = new List<User>();
}
