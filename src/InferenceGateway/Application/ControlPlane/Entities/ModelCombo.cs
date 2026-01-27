namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities;

public sealed class ModelCombo
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string OrderedModelsJson { get; set; } = "[]";

    public Tenant? Tenant { get; set; }
}
