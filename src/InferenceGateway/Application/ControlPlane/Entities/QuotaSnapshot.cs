namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities;

public sealed class QuotaSnapshot
{
    public Guid Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string QuotaJson { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
