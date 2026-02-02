namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities;

/// <summary>
/// Audit log for configuration changes.
/// Provides full accountability for who changed what and when.
/// </summary>
public record ConfigurationChangeLog
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>Type of configuration changed</summary>
    public string ChangeType { get; init; } = null!;  // Provider, RoutingPolicy, etc.
    
    /// <summary>Entity identifier that was changed</summary>
    public string EntityId { get; init; } = null!;
    
    /// <summary>Action performed: Create, Update, Delete</summary>
    public string Action { get; init; } = null!;
    
    /// <summary>JSON snapshot of previous state</summary>
    public string? PreviousValue { get; init; }
    
    /// <summary>JSON snapshot of new state</summary>
    public string? NewValue { get; init; }
    
    /// <summary>Username of who made the change</summary>
    public string ChangedBy { get; init; } = null!;
    
    /// <summary>Tenant ID context</summary>
    public string? TenantId { get; init; }
    
    /// <summary>Timestamp of change</summary>
    public DateTime ChangedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>Additional notes or reason</summary>
    public string? Notes { get; init; }
}
