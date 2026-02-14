// =============================================================================
// Stamp Metrics Service Interface
// =============================================================================

using Synaxis.StampController.CRDs;

namespace Synaxis.StampController.Services;

/// <summary>
/// Service for collecting stamp metrics
/// </summary>
public interface IStampMetricsService
{
    /// <summary>
    /// Gets current metrics for a stamp
    /// </summary>
    Task<StampMetrics> GetMetricsAsync(StampResource stamp, CancellationToken cancellationToken);

    /// <summary>
    /// Exposes Prometheus metrics
    /// </summary>
    Task<string> GetPrometheusMetricsAsync(CancellationToken cancellationToken);
}
