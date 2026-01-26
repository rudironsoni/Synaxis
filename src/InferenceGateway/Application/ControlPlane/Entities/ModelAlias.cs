namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities;

public sealed class ModelAlias
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Alias { get; set; } = string.Empty;
    public string TargetModel { get; set; } = string.Empty;

    public Tenant? Tenant { get; set; }
}
