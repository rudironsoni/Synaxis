using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Synaxis.InferenceGateway.Infrastructure.ControlPlane;

namespace Synaxis.InferenceGateway.Infrastructure.Agents.Tools;

public class ProviderTool : IProviderTool
{
    private readonly ControlPlaneDbContext _db;
    private readonly ILogger<ProviderTool> _logger;

    public ProviderTool(ControlPlaneDbContext db, ILogger<ProviderTool> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<bool> UpdateProviderConfigAsync(Guid organizationId, Guid providerId, string key, object value, CancellationToken ct = default)
    {
        try
        {
            // This is a placeholder - actual implementation would update OrganizationProvider settings
            _logger.LogInformation("UpdateProviderConfig: OrgId={OrgId}, ProviderId={ProviderId}, Key={Key}", 
                organizationId, providerId, key);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update provider config");
            return false;
        }
    }

    public async Task<ProviderStatus> GetProviderStatusAsync(Guid organizationId, Guid providerId, CancellationToken ct = default)
    {
        try
        {
            // Query from Operations schema - ProviderHealthStatus
            var healthStatus = await _db.Database.SqlQuery<ProviderHealthStatusDto>(
                $"SELECT \"IsHealthy\", \"LastCheckedAt\" FROM operations.\"ProviderHealthStatus\" WHERE \"OrganizationProviderId\" = {providerId} ORDER BY \"LastCheckedAt\" DESC LIMIT 1"
            ).FirstOrDefaultAsync(ct);

            return new ProviderStatus(
                true, // IsEnabled - would need to query OrganizationProvider
                healthStatus?.IsHealthy ?? true,
                healthStatus?.LastCheckedAt
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get provider status");
            return new ProviderStatus(false, false, null);
        }
    }

    public async Task<List<ProviderInfo>> GetAllProvidersAsync(Guid organizationId, CancellationToken ct = default)
    {
        try
        {
            // This would query OrganizationProvider from Operations schema
            return new List<ProviderInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all providers");
            return new List<ProviderInfo>();
        }
    }

    private class ProviderHealthStatusDto
    {
        public bool IsHealthy { get; set; }
        public DateTime LastCheckedAt { get; set; }
    }
}
