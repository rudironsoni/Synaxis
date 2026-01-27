namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities;

public sealed class OAuthAccount
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public byte[] AccessTokenEncrypted { get; set; } = Array.Empty<byte>();
    public byte[]? RefreshTokenEncrypted { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public OAuthAccountStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Tenant? Tenant { get; set; }
}
