namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities;

public sealed class RequestLog
{
    public Guid Id { get; set; }
    public string RequestId { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? UserId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string? Model { get; set; }
    public string? Provider { get; set; }
    public int? LatencyMs { get; set; }
    public int? StatusCode { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Tenant? Tenant { get; set; }
    public Project? Project { get; set; }
    public User? User { get; set; }
}
