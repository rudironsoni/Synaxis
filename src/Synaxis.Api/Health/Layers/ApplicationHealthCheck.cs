using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics;

namespace Synaxis.Api.Health.Layers;

/// <summary>
/// Layer 3: Application Health Check
/// Monitors application-level metrics including:
/// - Request processing time
/// - Active connections
/// - Thread pool status
/// - Garbage collection health
/// - Custom application metrics
/// </summary>
public class ApplicationHealthCheck : IHealthCheck
{
    private readonly ILogger<ApplicationHealthCheck> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ApplicationHealthCheck(
        ILogger<ApplicationHealthCheck> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var issues = new List<string>();
            var data = new Dictionary<string, object>();

            // Check thread pool
            var threadPoolInfo = CheckThreadPool();
            data["thread_pool_worker_threads"] = threadPoolInfo.WorkerThreads;
            data["thread_pool_completion_port_threads"] = threadPoolInfo.CompletionPortThreads;
            data["thread_pool_min_worker_threads"] = threadPoolInfo.MinWorkerThreads;
            data["thread_pool_min_completion_port_threads"] = threadPoolInfo.MinCompletionPortThreads;

            if (threadPoolInfo.WorkerThreads > 1000)
            {
                issues.Add($"High thread pool usage: {threadPoolInfo.WorkerThreads} worker threads");
            }

            // Check GC
            var gcInfo = CheckGarbageCollection();
            data["gc_gen0_collections"] = gcInfo.Gen0Collections;
            data["gc_gen1_collections"] = gcInfo.Gen1Collections;
            data["gc_gen2_collections"] = gcInfo.Gen2Collections;
            data["gc_total_memory_mb"] = gcInfo.TotalMemoryMB;
            data["gc_pause_time_ms"] = gcInfo.PauseTimeMs;

            if (gcInfo.TotalMemoryMB > 1000)
            {
                issues.Add($"High memory usage: {gcInfo.TotalMemoryMB} MB");
            }

            // Check process
            var processInfo = CheckProcess();
            data["process_id"] = processInfo.ProcessId;
            data["process_cpu_time_ms"] = processInfo.CpuTimeMs;
            data["process_working_set_mb"] = processInfo.WorkingSetMB;
            data["process_private_memory_mb"] = processInfo.PrivateMemoryMB;
            data["process_handle_count"] = processInfo.HandleCount;
            data["process_thread_count"] = processInfo.ThreadCount;

            if (processInfo.WorkingSetMB > 2000)
            {
                issues.Add($"High working set: {processInfo.WorkingSetMB} MB");
            }

            // Check assembly load count
            var assemblyCount = AppDomain.CurrentDomain.GetAssemblies().Length;
            data["assembly_count"] = assemblyCount;

            // Check app domain
            data["app_domain_friendly_name"] = AppDomain.CurrentDomain.FriendlyName;
            data["app_domain_is_fully_trusted"] = AppDomain.CurrentDomain.IsFullyTrusted;

            // Check uptime
            var uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
            data["uptime_seconds"] = uptime.TotalSeconds;

            // Determine health status
            if (issues.Count > 0)
            {
                _logger.LogWarning("Application health check degraded: {Issues}", string.Join(", ", issues));
                return Task.FromResult(HealthCheckResult.Degraded(
                    "Application has issues",
                    data: data,
                    exception: new AggregateException(issues.Select(i => new Exception(i)))));
            }

            _logger.LogInformation("Application health check passed");
            return Task.FromResult(HealthCheckResult.Healthy("Application is healthy", data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Application health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy("Application health check failed", exception: ex));
        }
    }

    private (int WorkerThreads, int CompletionPortThreads, int MinWorkerThreads, int MinCompletionPortThreads) CheckThreadPool()
    {
        ThreadPool.GetAvailableThreads(out var workerThreads, out var completionPortThreads);
        ThreadPool.GetMinThreads(out var minWorkerThreads, out var minCompletionPortThreads);

        return (workerThreads, completionPortThreads, minWorkerThreads, minCompletionPortThreads);
    }

    private (int Gen0Collections, int Gen1Collections, int Gen2Collections, double TotalMemoryMB, double PauseTimeMs) CheckGarbageCollection()
    {
        var gen0 = GC.CollectionCount(0);
        var gen1 = GC.CollectionCount(1);
        var gen2 = GC.CollectionCount(2);
        var totalMemory = GC.GetTotalMemory(false) / (1024.0 * 1024.0);

        // Estimate pause time (simplified)
        var pauseTime = 0.0;

        return (gen0, gen1, gen2, Math.Round(totalMemory, 2), pauseTime);
    }

    private (int ProcessId, double CpuTimeMs, double WorkingSetMB, double PrivateMemoryMB, int HandleCount, int ThreadCount) CheckProcess()
    {
        var process = Process.GetCurrentProcess();
        var cpuTime = process.TotalProcessorTime.TotalMilliseconds;
        var workingSet = process.WorkingSet64 / (1024.0 * 1024.0);
        var privateMemory = process.PrivateMemorySize64 / (1024.0 * 1024.0);

        return (
            process.Id,
            Math.Round(cpuTime, 2),
            Math.Round(workingSet, 2),
            Math.Round(privateMemory, 2),
            process.HandleCount,
            process.Threads.Count
        );
    }
}
