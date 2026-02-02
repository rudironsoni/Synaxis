using Synaxis.InferenceGateway.Application.Configuration;

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities;

/// <summary>
/// Represents a user's request to add a custom provider (BYOK).
/// Requires administrator approval before activation.
/// </summary>
public record ProviderRequest
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string TenantId { get; init; } = null!;
    public string UserId { get; init; } = null!;
    public ProviderConfig ProposedConfig { get; init; } = null!;
    
    /// <summary>
    /// Current approval workflow status.
    /// </summary>
    public ProviderRequestStatus Status { get; init; }
    
    /// <summary>
    /// Administrator notes during review.
    /// </summary>
    public string? AdminNote { get; init; }
    
    public DateTime CreatedAt { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? ReviewedBy { get; init; }
    
    /// <summary>
    /// Results from automated health check performed after approval.
    /// </summary>
    public HealthCheckResult? HealthCheckResult { get; init; }
    
    /// <summary>
    /// Results from sandbox testing performed before activation.
    /// </summary>
    public SandboxTestResult? SandboxTestResult { get; init; }
}

public enum ProviderRequestStatus
{
    /// <summary>Initial state - awaiting admin review</summary>
    Pending,
    
    /// <summary>Approved by admin, awaiting health check</summary>
    PendingHealthCheck,
    
    /// <summary>Admin approved, health check passed</summary>
    Approved,
    
    /// <summary>Admin rejected</summary>
    Rejected,
    
    /// <summary>Actively routing requests</summary>
    Active,
    
    /// <summary>Deactivated by admin</summary>
    Deactivated
}
