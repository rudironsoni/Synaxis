namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane.Entities.Identity;

/// <summary>
/// Represents organization-level settings.
/// </summary>
public class OrganizationSettings
{
    public Guid OrganizationId { get; set; }
    public int JwtTokenLifetimeMinutes { get; set; } = 60;
    public long MaxRequestBodySizeBytes { get; set; } = 10485760; // 10MB
    public int? DefaultRateLimitRpm { get; set; }
    public int? DefaultRateLimitTpm { get; set; }
    public bool AllowAutoOptimization { get; set; } = true;
    public bool AllowCustomProviders { get; set; } = false;
    public bool AllowAuditLogExport { get; set; } = false;
    public int? MaxUsers { get; set; }
    public int? MaxGroups { get; set; }
    public long? MonthlyTokenQuota { get; set; }
    public int AuditLogRetentionDays { get; set; } = 90;
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
}
