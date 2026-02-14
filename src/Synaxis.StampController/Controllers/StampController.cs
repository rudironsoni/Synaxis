// =============================================================================
// Stamp Lifecycle Controller
// Kubernetes Controller for managing ephemeral stamp lifecycle
// =============================================================================

using System.Collections.Concurrent;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Synaxis.StampController.CRDs;
using Synaxis.StampController.Services;

namespace Synaxis.StampController.Controllers;

/// <summary>
/// Main controller that watches for Stamp resources and reconciles their state
/// </summary>
public class StampController : BackgroundService
{
    private readonly IKubernetes _kubernetes;
    private readonly IStampLifecycleService _lifecycleService;
    private readonly IStampHealthService _healthService;
    private readonly IStampMetricsService _metricsService;
    private readonly ILogger<StampController> _logger;
    private readonly TimeSpan _reconciliationInterval = TimeSpan.FromSeconds(30);
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _stampMonitors = new();

    public StampController(
        IKubernetes kubernetes,
        IStampLifecycleService lifecycleService,
        IStampHealthService healthService,
        IStampMetricsService metricsService,
        ILogger<StampController> logger)
    {
        _kubernetes = kubernetes;
        _lifecycleService = lifecycleService;
        _healthService = healthService;
        _metricsService = metricsService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Stamp Controller starting...");

        // Ensure CRD exists
        await EnsureCrdExistsAsync(stoppingToken);

        // Start watching for Stamp resources
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReconcileStampsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during stamp reconciliation");
            }

            await Task.Delay(_reconciliationInterval, stoppingToken);
        }

        _logger.LogInformation("Stamp Controller stopping...");
    }

    /// <summary>
    /// Ensures the Stamp CRD exists in the cluster
    /// </summary>
    private async Task EnsureCrdExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var crd = StampCrdDefinition.CreateCrd();
            var existing = await _kubernetes.ApiextensionsV1.ReadCustomResourceDefinitionAsync(
                $"{StampCrdDefinition.Plural}.{StampCrdDefinition.Group}",
                cancellationToken: cancellationToken);

            _logger.LogInformation("Stamp CRD already exists");
        }
        catch (k8s.Exceptions.KubernetesClientException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogInformation("Creating Stamp CRD...");
            var crd = StampCrdDefinition.CreateCrd();
            await _kubernetes.ApiextensionsV1.CreateCustomResourceDefinitionAsync(crd, cancellationToken);
            _logger.LogInformation("Stamp CRD created successfully");
        }
    }

    /// <summary>
    /// Main reconciliation loop - processes all stamps
    /// </summary>
    private async Task ReconcileStampsAsync(CancellationToken cancellationToken)
    {
        var stamps = await ListStampsAsync(cancellationToken);

        foreach (var stamp in stamps)
        {
            try
            {
                await ReconcileStampAsync(stamp, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reconciling stamp {StampName}", stamp.Metadata.Name);
                await UpdateStampStatusAsync(stamp, StampPhase.Failed, ex.Message, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Reconciles a single stamp based on its current phase
    /// </summary>
    private async Task ReconcileStampAsync(StampResource stamp, CancellationToken cancellationToken)
    {
        var currentPhase = stamp.Status?.Phase ?? StampPhase.Pending;
        var stampKey = $"{stamp.Metadata.Namespace}/{stamp.Metadata.Name}";

        _logger.LogDebug("Reconciling stamp {StampName} in phase {Phase}",
            stamp.Metadata.Name, currentPhase);

        switch (currentPhase)
        {
            case StampPhase.Pending:
                await HandlePendingPhaseAsync(stamp, cancellationToken);
                break;

            case StampPhase.Provisioning:
                await HandleProvisioningPhaseAsync(stamp, cancellationToken);
                break;

            case StampPhase.Ready:
                await HandleReadyPhaseAsync(stamp, cancellationToken);
                break;

            case StampPhase.Scaling:
                await HandleScalingPhaseAsync(stamp, cancellationToken);
                break;

            case StampPhase.Degraded:
                await HandleDegradedPhaseAsync(stamp, cancellationToken);
                break;

            case StampPhase.Draining:
                await HandleDrainingPhaseAsync(stamp, cancellationToken);
                break;

            case StampPhase.Quarantine:
                await HandleQuarantinePhaseAsync(stamp, cancellationToken);
                break;

            case StampPhase.Decommissioning:
                await HandleDecommissioningPhaseAsync(stamp, cancellationToken);
                break;

            case StampPhase.Archived:
                await HandleArchivedPhaseAsync(stamp, cancellationToken);
                break;

            case StampPhase.Terminating:
                await HandleTerminatingPhaseAsync(stamp, cancellationToken);
                break;

            case StampPhase.Failed:
                // Failed stamps require manual intervention
                await MonitorFailedStampAsync(stamp, cancellationToken);
                break;

            default:
                _logger.LogWarning("Unknown stamp phase {Phase} for stamp {StampName}",
                    currentPhase, stamp.Metadata.Name);
                break;
        }
    }

    #region Phase Handlers

    private async Task HandlePendingPhaseAsync(StampResource stamp, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stamp {StampName} transitioning from Pending to Provisioning", stamp.Metadata.Name);

        await UpdateStampStatusAsync(stamp, StampPhase.Provisioning, "Starting provisioning", cancellationToken);

        // Start async provisioning
        _ = Task.Run(async () =>
        {
            try
            {
                await _lifecycleService.ProvisionStampAsync(stamp, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Provisioning failed for stamp {StampName}", stamp.Metadata.Name);
                await UpdateStampStatusAsync(stamp, StampPhase.Failed, $"Provisioning failed: {ex.Message}", cancellationToken);
            }
        }, cancellationToken);
    }

    private async Task HandleProvisioningPhaseAsync(StampResource stamp, CancellationToken cancellationToken)
    {
        // Check provisioning progress
        var status = await _lifecycleService.CheckProvisioningStatusAsync(stamp, cancellationToken);

        if (status.IsComplete)
        {
            _logger.LogInformation("Stamp {StampName} provisioning complete, transitioning to Ready", stamp.Metadata.Name);

            stamp.Status ??= new StampStatus();
            stamp.Status.ProvisionedAt = DateTime.UtcNow;

            // Set expiration if TTL is enabled
            if (stamp.Spec.TTL.Enabled)
            {
                stamp.Status.ExpiresAt = DateTime.UtcNow.AddHours(stamp.Spec.TTL.DurationHours);
            }

            await UpdateStampStatusAsync(stamp, StampPhase.Ready, "Provisioning complete", cancellationToken);

            // Start health monitoring
            StartHealthMonitoring(stamp, cancellationToken);
        }
        else if (status.HasFailed)
        {
            _logger.LogError("Stamp {StampName} provisioning failed: {Error}", stamp.Metadata.Name, status.ErrorMessage);
            await UpdateStampStatusAsync(stamp, StampPhase.Failed, status.ErrorMessage, cancellationToken);
        }
        else
        {
            // Still provisioning - update progress
            await UpdateStampConditionAsync(stamp, "Provisioning", ConditionStatus.True,
                "InProgress", $"Progress: {status.ProgressPercent}%", cancellationToken);
        }
    }

    private async Task HandleReadyPhaseAsync(StampResource stamp, CancellationToken cancellationToken)
    {
        // Check if stamp has expired
        if (stamp.Status?.ExpiresAt.HasValue == true &&
            DateTime.UtcNow > stamp.Status.ExpiresAt.Value)
        {
            _logger.LogInformation("Stamp {StampName} has expired, initiating drain", stamp.Metadata.Name);
            await UpdateStampStatusAsync(stamp, StampPhase.Draining, "Stamp expired", cancellationToken);
            return;
        }

        // Check health
        var health = await _healthService.CheckHealthAsync(stamp, cancellationToken);

        if (health.Overall == HealthStatus.Unhealthy)
        {
            _logger.LogWarning("Stamp {StampName} is unhealthy, transitioning to Degraded", stamp.Metadata.Name);
            stamp.Status ??= new StampStatus();
            stamp.Status.Health = health;
            await UpdateStampStatusAsync(stamp, StampPhase.Degraded, health.Message, cancellationToken);
        }
        else if (health.Overall == HealthStatus.Degraded)
        {
            stamp.Status ??= new StampStatus();
            stamp.Status.Health = health;
            await UpdateStampConditionAsync(stamp, "Health", ConditionStatus.False, "Degraded", health.Message, cancellationToken);
        }
        else
        {
            await UpdateStampConditionAsync(stamp, "Health", ConditionStatus.True, "Healthy", "All health checks passing", cancellationToken);
        }

        // Check if scaling is needed
        var metrics = await _metricsService.GetMetricsAsync(stamp, cancellationToken);
        stamp.Status ??= new StampStatus();
        stamp.Status.Metrics = metrics;

        if (stamp.Spec.AutoScaling.Enabled)
        {
            var scalingAction = await _lifecycleService.EvaluateScalingAsync(stamp, metrics, cancellationToken);

            if (scalingAction.Action != ScalingAction.None)
            {
                _logger.LogInformation("Stamp {StampName} requires {Action} scaling", stamp.Metadata.Name, scalingAction.Action);
                await UpdateStampStatusAsync(stamp, StampPhase.Scaling, $"Scaling {scalingAction.Action}", cancellationToken);
            }
        }
    }

    private async Task HandleScalingPhaseAsync(StampResource stamp, CancellationToken cancellationToken)
    {
        var result = await _lifecycleService.ExecuteScalingAsync(stamp, cancellationToken);

        if (result.Success)
        {
            _logger.LogInformation("Stamp {StampName} scaling complete", stamp.Metadata.Name);
            await UpdateStampStatusAsync(stamp, StampPhase.Ready, "Scaling complete", cancellationToken);
        }
        else
        {
            _logger.LogError("Stamp {StampName} scaling failed: {Error}", stamp.Metadata.Name, result.ErrorMessage);
            await UpdateStampStatusAsync(stamp, StampPhase.Degraded, $"Scaling failed: {result.ErrorMessage}", cancellationToken);
        }
    }

    private async Task HandleDegradedPhaseAsync(StampResource stamp, CancellationToken cancellationToken)
    {
        // Attempt automatic recovery
        var recoveryResult = await _lifecycleService.AttemptRecoveryAsync(stamp, cancellationToken);

        if (recoveryResult.Success)
        {
            _logger.LogInformation("Stamp {StampName} recovered from degraded state", stamp.Metadata.Name);
            await UpdateStampStatusAsync(stamp, StampPhase.Ready, "Recovery successful", cancellationToken);
        }
        else
        {
            // If recovery failed, start draining
            _logger.LogWarning("Stamp {StampName} recovery failed, initiating drain", stamp.Metadata.Name);
            await UpdateStampStatusAsync(stamp, StampPhase.Draining, "Automatic recovery failed", cancellationToken);
        }
    }

    private async Task HandleDrainingPhaseAsync(StampResource stamp, CancellationToken cancellationToken)
    {
        // Check if drain timeout has elapsed
        var drainingCondition = stamp.Status?.Conditions?.FirstOrDefault(c => c.Type == "Draining");
        var drainStartTime = drainingCondition?.LastTransitionTime ?? DateTime.UtcNow;
        var elapsed = DateTime.UtcNow - drainStartTime;

        if (elapsed.TotalMinutes >= stamp.Spec.DrainTimeoutMinutes)
        {
            _logger.LogInformation("Stamp {StampName} drain timeout reached, transitioning to Quarantine", stamp.Metadata.Name);
            await UpdateStampStatusAsync(stamp, StampPhase.Quarantine, "Drain timeout reached", cancellationToken);
            return;
        }

        // Check if drain is complete
        var drainStatus = await _lifecycleService.CheckDrainStatusAsync(stamp, cancellationToken);

        if (drainStatus.IsComplete)
        {
            _logger.LogInformation("Stamp {StampName} drain complete", stamp.Metadata.Name);
            await UpdateStampStatusAsync(stamp, StampPhase.Quarantine, "Drain complete", cancellationToken);
        }
        else
        {
            await UpdateStampConditionAsync(stamp, "Draining", ConditionStatus.True, "InProgress",
                $"Draining connections: {drainStatus.RemainingConnections} remaining", cancellationToken);
        }
    }

    private async Task HandleQuarantinePhaseAsync(StampResource stamp, CancellationToken cancellationToken)
    {
        var quarantineCondition = stamp.Status?.Conditions?.FirstOrDefault(c => c.Type == "Quarantine");
        var quarantineStartTime = quarantineCondition?.LastTransitionTime ?? DateTime.UtcNow;
        var elapsed = DateTime.UtcNow - quarantineStartTime;

        if (elapsed.TotalMinutes >= stamp.Spec.QuarantineTimeoutMinutes)
        {
            _logger.LogInformation("Stamp {StampName} quarantine timeout reached, transitioning to Decommissioning", stamp.Metadata.Name);
            await UpdateStampStatusAsync(stamp, StampPhase.Decommissioning, "Quarantine complete", cancellationToken);
        }
    }

    private async Task HandleDecommissioningPhaseAsync(StampResource stamp, CancellationToken cancellationToken)
    {
        var result = await _lifecycleService.DecommissionStampAsync(stamp, cancellationToken);

        if (result.Success)
        {
            _logger.LogInformation("Stamp {StampName} decommissioning complete", stamp.Metadata.Name);
            await UpdateStampStatusAsync(stamp, StampPhase.Archived, "Decommissioning complete", cancellationToken);
        }
        else
        {
            _logger.LogError("Stamp {StampName} decommissioning failed: {Error}", stamp.Metadata.Name, result.ErrorMessage);
            await UpdateStampStatusAsync(stamp, StampPhase.Failed, $"Decommissioning failed: {result.ErrorMessage}", cancellationToken);
        }
    }

    private async Task HandleArchivedPhaseAsync(StampResource stamp, CancellationToken cancellationToken)
    {
        // Archived stamps wait for data retention before terminating
        var archiveAge = DateTime.UtcNow - (stamp.Status?.LastHealthCheck ?? DateTime.UtcNow);

        if (archiveAge.TotalDays >= 7) // Archive for 7 days before purge
        {
            _logger.LogInformation("Stamp {StampName} archive retention period complete, transitioning to Terminating", stamp.Metadata.Name);
            await UpdateStampStatusAsync(stamp, StampPhase.Terminating, "Archive retention complete", cancellationToken);
        }
    }

    private async Task HandleTerminatingPhaseAsync(StampResource stamp, CancellationToken cancellationToken)
    {
        // Final cleanup and resource deletion
        await _lifecycleService.TerminateStampAsync(stamp, cancellationToken);

        // Stop health monitoring
        var stampKey = $"{stamp.Metadata.Namespace}/{stamp.Metadata.Name}";
        if (_stampMonitors.TryRemove(stampKey, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }

        _logger.LogInformation("Stamp {StampName} terminated", stamp.Metadata.Name);

        // Delete the Stamp CR
        await _kubernetes.DeleteNamespacedCustomObjectAsync(
            StampCrdDefinition.Group,
            StampCrdDefinition.Version,
            stamp.Metadata.Namespace,
            StampCrdDefinition.Plural,
            stamp.Metadata.Name,
            cancellationToken: cancellationToken);
    }

    private async Task MonitorFailedStampAsync(StampResource stamp, CancellationToken cancellationToken)
    {
        // Failed stamps require manual intervention
        // Just update last health check timestamp
        if (stamp.Status != null)
        {
            stamp.Status.LastHealthCheck = DateTime.UtcNow;
            await UpdateStampStatusAsync(stamp, StampPhase.Failed, stamp.Status.Health?.Message ?? "Failed", cancellationToken);
        }
    }

    #endregion

    #region Helper Methods

    private void StartHealthMonitoring(StampResource stamp, CancellationToken cancellationToken)
    {
        var stampKey = $"{stamp.Metadata.Namespace}/{stamp.Metadata.Name}";

        if (_stampMonitors.ContainsKey(stampKey))
            return;

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _stampMonitors[stampKey] = cts;

        _ = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    var health = await _healthService.CheckHealthAsync(stamp, cts.Token);
                    var metrics = await _metricsService.GetMetricsAsync(stamp, cts.Token);

                    stamp.Status ??= new StampStatus();
                    stamp.Status.Health = health;
                    stamp.Status.Metrics = metrics;
                    stamp.Status.LastHealthCheck = DateTime.UtcNow;

                    await UpdateStampStatusAsync(stamp, stamp.Status.Phase, null, cts.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Health monitoring failed for stamp {StampName}", stamp.Metadata.Name);
                }

                await Task.Delay(TimeSpan.FromSeconds(30), cts.Token);
            }
        }, cts.Token);
    }

    private async Task<List<StampResource>> ListStampsAsync(CancellationToken cancellationToken)
    {
        var result = await _kubernetes.ListNamespacedCustomObjectAsync<StampResource>(
            StampCrdDefinition.Group,
            StampCrdDefinition.Version,
            "default", // TODO: Support multiple namespaces
            StampCrdDefinition.Plural,
            cancellationToken: cancellationToken);

        return result.Items?.ToList() ?? new List<StampResource>();
    }

    private async Task UpdateStampStatusAsync(StampResource stamp, StampPhase phase, string? message, CancellationToken cancellationToken)
    {
        stamp.Status ??= new StampStatus();
        stamp.Status.Phase = phase;
        stamp.Status.ObservedGeneration = stamp.Metadata.Generation ?? 0;

        if (!string.IsNullOrEmpty(message))
        {
            stamp.Status.Health ??= new StampHealth();
            stamp.Status.Health.Message = message;
        }

        try
        {
            await _kubernetes.PatchNamespacedCustomObjectStatusAsync<StampResource>(
                new V1Patch(stamp, V1Patch.PatchType.MergePatch),
                StampCrdDefinition.Group,
                StampCrdDefinition.Version,
                stamp.Metadata.Namespace,
                StampCrdDefinition.Plural,
                stamp.Metadata.Name,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update stamp status for {StampName}", stamp.Metadata.Name);
        }
    }

    private async Task UpdateStampConditionAsync(StampResource stamp, string conditionType, ConditionStatus status,
        string reason, string message, CancellationToken cancellationToken)
    {
        stamp.Status ??= new StampStatus();
        stamp.Status.Conditions ??= new List<StampCondition>();

        var condition = stamp.Status.Conditions.FirstOrDefault(c => c.Type == conditionType);

        if (condition == null)
        {
            condition = new StampCondition
            {
                Type = conditionType,
                LastTransitionTime = DateTime.UtcNow
            };
            stamp.Status.Conditions.Add(condition);
        }
        else if (condition.Status != status)
        {
            condition.LastTransitionTime = DateTime.UtcNow;
        }

        condition.Status = status;
        condition.Reason = reason;
        condition.Message = message;

        await UpdateStampStatusAsync(stamp, stamp.Status.Phase, null, cancellationToken);
    }

    #endregion
}

/// <summary>
/// Scaling action types
/// </summary>
public enum ScalingAction
{
    None,
    ScaleUp,
    ScaleDown
}

/// <summary>
/// Scaling evaluation result
/// </summary>
public class ScalingEvaluation
{
    public ScalingAction Action { get; set; }
    public int TargetNodes { get; set; }
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Provisioning status
/// </summary>
public class ProvisioningStatus
{
    public bool IsComplete { get; set; }
    public bool HasFailed { get; set; }
    public int ProgressPercent { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Drain status
/// </summary>
public class DrainStatus
{
    public bool IsComplete { get; set; }
    public int RemainingConnections { get; set; }
}

/// <summary>
/// Operation result
/// </summary>
public class OperationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
