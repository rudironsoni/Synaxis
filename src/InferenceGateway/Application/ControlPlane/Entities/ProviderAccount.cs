namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities;

public sealed class ProviderAccount
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public ProviderAccountStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Tenant? Tenant { get; set; }
}
