namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane;

public sealed class ControlPlaneOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string Region { get; set; } = "us";
    public bool UseInMemory { get; set; }
}
