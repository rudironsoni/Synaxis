// =============================================================================
// Stamp Health Service Implementation
// =============================================================================

using k8s;
using Microsoft.Extensions.Logging;
using Synaxis.StampController.CRDs;

namespace Synaxis.StampController.Services;

/// <summary>
/// Implementation of stamp health checking
/// </summary>
public class StampHealthService : IStampHealthService
{
    private readonly IKubernetes _kubernetes;
    private readonly ILogger<StampHealthService> _logger;

    public StampHealthService(IKubernetes kubernetes, ILogger<StampHealthService> logger)
    {
        _kubernetes = kubernetes;
        _logger = logger;
    }

    public async Task<StampHealth> CheckHealthAsync(StampResource stamp, CancellationToken cancellationToken)
    {
        var health = new StampHealth();

        try
        {
            // Check Kubernetes health
            health.Kubernetes = await CheckKubernetesHealthAsync(stamp, cancellationToken);

            // Check networking health
            health.Networking = await CheckNetworkingHealthAsync(stamp, cancellationToken);

            // Check storage health
            health.Storage = await CheckStorageHealthAsync(stamp, cancellationToken);

            // Determine overall health
            health.Overall = DetermineOverallHealth(health);

            return health;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for stamp {StampName}", stamp.Metadata.Name);
            return new StampHealth
            {
                Overall = HealthStatus.Unhealthy,
                Message = $"Health check failed: {ex.Message}"
            };
        }
    }

    public async Task<HealthStatus> CheckKubernetesHealthAsync(StampResource stamp, CancellationToken cancellationToken)
    {
        try
        {
            // Check if the namespace exists
            var ns = await _kubernetes.CoreV1.ReadNamespaceAsync(
                $"stamp-{stamp.Metadata.Name}",
                cancellationToken: cancellationToken);

            if (ns == null)
            {
                return HealthStatus.Unhealthy;
            }

            // Check pods in the namespace
            var pods = await _kubernetes.CoreV1.ListNamespacedPodAsync(
                $"stamp-{stamp.Metadata.Name}",
                cancellationToken: cancellationToken);

            var totalPods = pods.Items.Count;
            var readyPods = pods.Items.Count(p =>
                p.Status?.ContainerStatuses?.All(c => c.Ready) == true);

            if (totalPods == 0)
            {
                return HealthStatus.Degraded;
            }

            var readyRatio = (double)readyPods / totalPods;

            if (readyRatio >= 0.9)
                return HealthStatus.Healthy;
            else if (readyRatio >= 0.5)
                return HealthStatus.Degraded;
            else
                return HealthStatus.Unhealthy;
        }
        catch (k8s.Exceptions.KubernetesClientException)
        {
            return HealthStatus.Unhealthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kubernetes health check failed");
            return HealthStatus.Unhealthy;
        }
    }

    public async Task<HealthStatus> CheckNetworkingHealthAsync(StampResource stamp, CancellationToken cancellationToken)
    {
        try
        {
            // Check if the ingress/service is accessible
            var services = await _kubernetes.CoreV1.ListNamespacedServiceAsync(
                $"stamp-{stamp.Metadata.Name}",
                cancellationToken: cancellationToken);

            var mainService = services.Items.FirstOrDefault(s => s.Metadata.Name == "synaxis-api");

            if (mainService == null)
            {
                return HealthStatus.Degraded;
            }

            // Check load balancer status
            if (mainService.Status?.LoadBalancer?.Ingress?.Any() != true)
            {
                return HealthStatus.Degraded;
            }

            return HealthStatus.Healthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Networking health check failed");
            return HealthStatus.Unhealthy;
        }
    }

    public async Task<HealthStatus> CheckStorageHealthAsync(StampResource stamp, CancellationToken cancellationToken)
    {
        try
        {
            // Check PVC status
            var pvcs = await _kubernetes.CoreV1.ListNamespacedPersistentVolumeClaimAsync(
                $"stamp-{stamp.Metadata.Name}",
                cancellationToken: cancellationToken);

            if (!pvcs.Items.Any())
            {
                // No PVCs means no persistent storage needed - healthy
                return HealthStatus.Healthy;
            }

            var boundPvcs = pvcs.Items.Count(p => p.Status?.Phase == "Bound");
            var totalPvcs = pvcs.Items.Count;

            if (boundPvcs == totalPvcs)
                return HealthStatus.Healthy;
            else if (boundPvcs > 0)
                return HealthStatus.Degraded;
            else
                return HealthStatus.Unhealthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Storage health check failed");
            return HealthStatus.Unhealthy;
        }
    }

    private static HealthStatus DetermineOverallHealth(StampHealth health)
    {
        var statuses = new[] { health.Kubernetes, health.Networking, health.Storage };

        if (statuses.All(s => s == HealthStatus.Healthy))
            return HealthStatus.Healthy;

        if (statuses.Any(s => s == HealthStatus.Unhealthy))
            return HealthStatus.Unhealthy;

        return HealthStatus.Degraded;
    }
}
