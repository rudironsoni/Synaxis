namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities;

public sealed class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TenantRegion Region { get; set; }
    public TenantStatus Status { get; set; }
    public Guid? ByokKeyId { get; set; }
    public byte[] EncryptedByokKey { get; set; } = Array.Empty<byte>();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Project> Projects { get; } = new List<Project>();
    public ICollection<User> Users { get; } = new List<User>();
    public ICollection<OAuthAccount> OAuthAccounts { get; } = new List<OAuthAccount>();
}
