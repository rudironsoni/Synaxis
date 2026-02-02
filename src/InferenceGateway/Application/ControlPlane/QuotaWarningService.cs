namespace Synaxis.InferenceGateway.Application.ControlPlane;

using Synaxis.InferenceGateway.Application.Routing;
using Microsoft.Extensions.Logging;

/// <summary>
/// Implements quota warning monitoring.
/// </summary>
public class QuotaWarningService : IQuotaWarningService
{
    private readonly IQuotaTracker _quotaTracker;
    private readonly INotificationService _notificationService;
    private readonly ILogger<QuotaWarningService> _logger;
    private const double WarningThreshold = 0.2; // 20%

    public QuotaWarningService(
        IQuotaTracker quotaTracker,
        INotificationService notificationService,
        ILogger<QuotaWarningService> logger)
    {
        _quotaTracker = quotaTracker ?? throw new ArgumentNullException(nameof(quotaTracker));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> CheckQuotaWarningAsync(
        string providerKey,
        string? tenantId = null,
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        // TODO: Get actual quota usage from IQuotaTracker
        // This is interim implementation - IQuotaTracker doesn't have a method to get remaining quota

        var remainingQuota = 100; // Placeholder
        var totalQuota = 1000; // Placeholder
        var usageRatio = (double)remainingQuota / totalQuota;

        if (usageRatio < WarningThreshold)
        {
            _logger.LogWarning("Quota warning for provider '{ProviderKey}': {RemainingQuota}/{TotalQuota} ({UsageRatio:P0}) remaining",
                providerKey, remainingQuota, totalQuota, usageRatio);

            await _notificationService.SendQuotaWarningAsync(
                tenantId ?? "default",
                userId ?? "default",
                providerKey,
                remainingQuota,
                cancellationToken);

            return true;
        }

        return false;
    }
}
