namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities;

public sealed class RoutingPolicy
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string PolicyJson { get; set; } = "{}";
    public int Version { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Tenant? Tenant { get; set; }
}
