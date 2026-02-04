namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities;

public sealed class ApiKey
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? UserId { get; set; }
    public string KeyHash { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ApiKeyStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTime? LastUsedAt { get; set; }
    public bool IsActive => Status == ApiKeyStatus.Active;

    public Project? Project { get; set; }
    public User? User { get; set; }
}
