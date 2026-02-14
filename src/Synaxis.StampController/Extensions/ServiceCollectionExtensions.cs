// =============================================================================
// Service Collection Extensions
// Dependency Injection configuration for Stamp Controller
// =============================================================================

using k8s;
using Microsoft.Extensions.Configuration;
using Prometheus;
using Synaxis.StampController.Controllers;
using Synaxis.StampController.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures all services required for the Stamp Controller
    /// </summary>
    public static IServiceCollection ConfigureStampController(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Kubernetes client
        services.AddSingleton<IKubernetes>(provider =>
        {
            var config = KubernetesClientConfiguration.BuildDefaultConfig();
            return new Kubernetes(config);
        });

        // Core services
        services.AddSingleton<IStampLifecycleService, StampLifecycleService>();
        services.AddSingleton<IStampHealthService, StampHealthService>();
        services.AddSingleton<IStampMetricsService, StampMetricsService>();
        services.AddSingleton<IStampRoutingService, StampRoutingService>();

        // Hosted services
        services.AddHostedService<StampController>();
        services.AddHostedService<StampHealthMonitor>();
        services.AddHostedService<StampMetricsCollector>();

        // Configuration
        services.Configure<StampControllerOptions>(configuration.GetSection("StampController"));

        // Health checks
        services.AddHealthChecks()
            .AddCheck<StampControllerHealthCheck>("stamp-controller");

        // Metrics
        services.AddMetricServer(options =>
        {
            options.Port = 9090;
            options.Url = "/metrics";
        });

        return services;
    }
}

/// <summary>
/// Stamp Controller configuration options
/// </summary>
public class StampControllerOptions
{
    /// <summary>
    /// Namespace to watch for Stamp resources
    /// </summary>
    public string Namespace { get; set; } = "default";

    /// <summary>
    /// Reconciliation interval in seconds
    /// </summary>
    public int ReconcileIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Health check interval in seconds
    /// </summary>
    public int HealthCheckIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Metrics collection interval in seconds
    /// </summary>
    public int MetricsIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum number of concurrent stamp operations
    /// </summary>
    public int MaxConcurrentOperations { get; set; } = 5;

    /// <summary>
    /// Default drain timeout in minutes
    /// </summary>
    public int DefaultDrainTimeoutMinutes { get; set; } = 30;

    /// <summary>
    /// Default quarantine timeout in minutes
    /// </summary>
    public int DefaultQuarantineTimeoutMinutes { get; set; } = 10;

    /// <summary>
    /// Enable automatic TTL enforcement
    /// </summary>
    public bool EnableTTL { get; set; } = true;

    /// <summary>
    /// Enable automatic scaling
    /// </summary>
    public bool EnableAutoScaling { get; set; } = true;

    /// <summary>
    /// Threshold for unhealthy stamp (error rate > x%)
    /// </summary>
    public double UnhealthyErrorThreshold { get; set; } = 0.05;

    /// <summary>
    /// Threshold for degraded stamp (latency p99 > x ms)
    /// </summary>
    public double DegradedLatencyThreshold { get; set; } = 1000;
}
