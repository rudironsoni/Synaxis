namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities;

public sealed class ApiKey
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string KeyHash { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ApiKeyStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Project? Project { get; set; }
}
