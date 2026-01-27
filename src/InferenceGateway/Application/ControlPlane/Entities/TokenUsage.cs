namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities;

public sealed class TokenUsage
{
    public Guid Id { get; set; }
    public string RequestId { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? UserId { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public decimal CostEstimate { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Tenant? Tenant { get; set; }
    public Project? Project { get; set; }
    public User? User { get; set; }
}
