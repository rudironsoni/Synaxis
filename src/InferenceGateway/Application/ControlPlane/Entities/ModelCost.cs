namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities;

public sealed class ModelCost
{
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public decimal CostPerToken { get; set; }
    public bool FreeTier { get; set; }
}
