namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities;

public sealed class DeviationEntry
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string Field { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Mitigation { get; set; } = string.Empty;
    public DeviationStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Tenant? Tenant { get; set; }
}
