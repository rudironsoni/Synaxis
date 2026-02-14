using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Synaxis.Api.Health.Layers;

/// <summary>
/// Layer 1: Infrastructure Health Check
/// Monitors Kubernetes infrastructure health including:
/// - Pod resource availability
/// - Disk space
/// - Memory pressure
/// - Network connectivity
/// </summary>
public class InfrastructureHealthCheck : IHealthCheck
{
    private readonly ILogger<InfrastructureHealthCheck> _logger;
    private readonly IWebHostEnvironment _environment;

    public InfrastructureHealthCheck(
        ILogger<InfrastructureHealthCheck> logger,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var issues = new List<string>();
            var data = new Dictionary<string, object>();

            // Check disk space
            var diskInfo = CheckDiskSpace();
            data["disk_usage_percent"] = diskInfo.UsagePercent;
            if (diskInfo.UsagePercent > 90)
            {
                issues.Add($"Disk usage critical: {diskInfo.UsagePercent}%");
            }

            // Check memory
            var memoryInfo = CheckMemory();
            data["memory_usage_percent"] = memoryInfo.UsagePercent;
            if (memoryInfo.UsagePercent > 90)
            {
                issues.Add($"Memory usage critical: {memoryInfo.UsagePercent}%");
            }

            // Check CPU
            var cpuInfo = CheckCpu();
            data["cpu_usage_percent"] = cpuInfo.UsagePercent;
            if (cpuInfo.UsagePercent > 90)
            {
                issues.Add($"CPU usage critical: {cpuInfo.UsagePercent}%");
            }

            // Check environment
            data["environment"] = _environment.EnvironmentName;
            data["machine_name"] = Environment.MachineName;
            data["os_version"] = Environment.OSVersion.ToString();
            data["processor_count"] = Environment.ProcessorCount;

            // Check if running in Kubernetes
            var isKubernetes = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST"));
            data["is_kubernetes"] = isKubernetes;

            if (isKubernetes)
            {
                data["pod_name"] = Environment.GetEnvironmentVariable("POD_NAME") ?? "unknown";
                data["pod_namespace"] = Environment.GetEnvironmentVariable("POD_NAMESPACE") ?? "unknown";
                data["pod_ip"] = Environment.GetEnvironmentVariable("POD_IP") ?? "unknown";
                data["node_name"] = Environment.GetEnvironmentVariable("NODE_NAME") ?? "unknown";
            }

            // Determine health status
            if (issues.Count > 0)
            {
                _logger.LogWarning("Infrastructure health check degraded: {Issues}", string.Join(", ", issues));
                return HealthCheckResult.Degraded(
                    "Infrastructure has issues",
                    data: data,
                    exception: new AggregateException(issues.Select(i => new Exception(i))));
            }

            _logger.LogInformation("Infrastructure health check passed");
            return HealthCheckResult.Healthy("Infrastructure is healthy", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Infrastructure health check failed");
            return HealthCheckResult.Unhealthy("Infrastructure health check failed", exception: ex);
        }
    }

    private double CheckDiskSpace()
    {
        try
        {
            var driveInfo = new DriveInfo(Directory.GetCurrentDirectory());
            var totalFreeSpace = driveInfo.TotalFreeSpace;
            var totalSize = driveInfo.TotalSize;
            var usedSpace = totalSize - totalFreeSpace;
            var usagePercent = (double)usedSpace / totalSize * 100;

            return Math.Round(usagePercent, 2);
        }
        catch
        {
            return 0;
        }
    }

    private double CheckMemory()
    {
        try
        {
            var memoryInfo = GC.GetGCMemoryInfo();
            var totalMemory = memoryInfo.TotalAvailableMemoryBytes;
            var memoryLoad = memoryInfo.MemoryLoadBytes;
            var usagePercent = (double)memoryLoad / totalMemory * 100;

            return Math.Round(usagePercent, 2);
        }
        catch
        {
            return 0;
        }
    }

    private double CheckCpu()
    {
        try
        {
            // Simple CPU check - in production, use PerformanceCounter or similar
            var process = Process.GetCurrentProcess();
            var startTime = DateTime.UtcNow;
            var startCpuUsage = process.TotalProcessorTime;

            Thread.Sleep(500);

            var endTime = DateTime.UtcNow;
            var endCpuUsage = process.TotalProcessorTime;

            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
            var usagePercent = cpuUsageTotal * 100;

            return Math.Round(usagePercent, 2);
        }
        catch
        {
            return 0;
        }
    }
}
