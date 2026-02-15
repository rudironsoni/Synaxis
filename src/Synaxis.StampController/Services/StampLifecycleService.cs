// <copyright file="StampLifecycleService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.StampController.Services;

using System.Text.Json;
using k8s.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Synaxis.StampController.Models;

/// <summary>
/// Service for managing stamp lifecycle using ConfigMaps.
/// </summary>
public sealed class StampLifecycleService
{
    private readonly KubernetesClientWrapper _kubernetesClient;
    private readonly ILogger<StampLifecycleService> _logger;
    private readonly StampLifecycleOptions _options;
    private readonly Dictionary<StampPhase, Func<StampConfigMap, CancellationToken, Task<StampPhase>>> _phaseHandlers;

    /// <summary>
    /// Initializes a new instance of the <see cref="StampLifecycleService"/> class.
    /// </summary>
    /// <param name="kubernetesClient">The Kubernetes client wrapper.</param>
    /// <param name="options">The stamp lifecycle options.</param>
    /// <param name="logger">The logger instance.</param>
    public StampLifecycleService(
        KubernetesClientWrapper kubernetesClient,
        IOptions<StampLifecycleOptions> options,
        ILogger<StampLifecycleService> logger)
    {
        this._kubernetesClient = kubernetesClient;
        this._logger = logger;
        this._options = options.Value;

        this._phaseHandlers = new Dictionary<StampPhase, Func<StampConfigMap, CancellationToken, Task<StampPhase>>>
        {
            { StampPhase.Provision, this.HandleProvisionPhaseAsync },
            { StampPhase.Register, this.HandleRegisterPhaseAsync },
            { StampPhase.Active, this.HandleActivePhaseAsync },
            { StampPhase.Drain, this.HandleDrainPhaseAsync },
            { StampPhase.Quarantine, this.HandleQuarantinePhaseAsync },
            { StampPhase.Decommission, this.HandleDecommissionPhaseAsync },
            { StampPhase.Archive, this.HandleArchivePhaseAsync },
            { StampPhase.Purge, this.HandlePurgePhaseAsync },
        };
    }

    /// <summary>
    /// Processes all stamps in their current phase.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task ProcessStampsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var configMaps = await this._kubernetesClient.ListConfigMapsAsync(cancellationToken).ConfigureAwait(false);

            foreach (var configMap in configMaps)
            {
                try
                {
                    var stamp = this.DeserializeStamp(configMap);
                    if (stamp == null)
                    {
                        this._logger.LogWarning("Failed to deserialize stamp from ConfigMap: {Name}", configMap.Metadata.Name);
                        continue;
                    }

                    await this.ProcessStampAsync(stamp, configMap, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, "Error processing stamp: {Name}", configMap.Metadata.Name);
                }
            }
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error listing stamps for processing");
        }
    }

    /// <summary>
    /// Processes a single stamp through its lifecycle.
    /// </summary>
    /// <param name="stamp">The stamp to process.</param>
    /// <param name="configMap">The ConfigMap representing the stamp.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private async Task ProcessStampAsync(StampConfigMap stamp, V1ConfigMap configMap, CancellationToken cancellationToken)
    {
        if (!this._phaseHandlers.TryGetValue(stamp.Phase, out var handler))
        {
            this._logger.LogWarning("No handler found for phase: {Phase} of stamp: {Id}", stamp.Phase, stamp.Id);
            return;
        }

        this._logger.LogInformation("Processing stamp: {Id} in phase: {Phase}", stamp.Id, stamp.Phase);

        try
        {
            var nextPhase = await handler(stamp, cancellationToken).ConfigureAwait(false);

            if (nextPhase != stamp.Phase)
            {
                this._logger.LogInformation("Transitioning stamp: {Id} from {OldPhase} to {NewPhase}", stamp.Id, stamp.Phase, nextPhase);
                stamp.Phase = nextPhase;
                stamp.Status = $"Transitioned to {nextPhase}";

                await this.UpdateStampAsync(stamp, configMap, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error processing stamp: {Id} in phase: {Phase}", stamp.Id, stamp.Phase);
            stamp.Status = $"Error: {ex.Message}";
            await this.UpdateStampAsync(stamp, configMap, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Handles the Provision phase.
    /// </summary>
    private async Task<StampPhase> HandleProvisionPhaseAsync(StampConfigMap stamp, CancellationToken cancellationToken)
    {
        this._logger.LogInformation("Provisioning stamp: {Id}", stamp.Id);

        // Simulate provisioning logic
        await Task.Delay(this._options.ProvisionDelayMs, cancellationToken).ConfigureAwait(false);

        stamp.Status = "Provisioned successfully";
        return StampPhase.Register;
    }

    /// <summary>
    /// Handles the Register phase.
    /// </summary>
    private async Task<StampPhase> HandleRegisterPhaseAsync(StampConfigMap stamp, CancellationToken cancellationToken)
    {
        this._logger.LogInformation("Registering stamp: {Id}", stamp.Id);

        // Simulate registration logic
        await Task.Delay(this._options.RegisterDelayMs, cancellationToken).ConfigureAwait(false);

        stamp.Status = "Registered successfully";
        return StampPhase.Active;
    }

    /// <summary>
    /// Handles the Active phase.
    /// </summary>
#pragma warning disable S1172 // Remove this unused method parameter '_'.
    private Task<StampPhase> HandleActivePhaseAsync(StampConfigMap stamp, CancellationToken _)
#pragma warning restore S1172 // Remove this unused method parameter '_'.
    {
        this._logger.LogInformation("Stamp {Id} is active", stamp.Id);

        // Check if TTL has expired
        if (stamp.Ttl.HasValue)
        {
            var age = DateTime.UtcNow - stamp.CreatedAt;
            if (age.TotalSeconds > stamp.Ttl.Value)
            {
                this._logger.LogInformation("Stamp {Id} TTL expired, transitioning to Drain", stamp.Id);
                stamp.Status = "TTL expired";
                return Task.FromResult(StampPhase.Drain);
            }
        }

        stamp.Status = "Active and serving traffic";
        return Task.FromResult(StampPhase.Active);
    }

    /// <summary>
    /// Handles the Drain phase.
    /// </summary>
    private async Task<StampPhase> HandleDrainPhaseAsync(StampConfigMap stamp, CancellationToken cancellationToken)
    {
        this._logger.LogInformation("Draining stamp: {Id}", stamp.Id);

        // Simulate draining logic
        await Task.Delay(this._options.DrainDelayMs, cancellationToken).ConfigureAwait(false);

        stamp.Status = "Drained successfully";
        return StampPhase.Decommission;
    }

    /// <summary>
    /// Handles the Quarantine phase.
    /// </summary>
    private async Task<StampPhase> HandleQuarantinePhaseAsync(StampConfigMap stamp, CancellationToken cancellationToken)
    {
        this._logger.LogInformation("Stamp {Id} is in quarantine", stamp.Id);

        // Simulate quarantine logic
        await Task.Delay(this._options.QuarantineDelayMs, cancellationToken).ConfigureAwait(false);

        stamp.Status = "Quarantine period ended";
        return StampPhase.Decommission;
    }

    /// <summary>
    /// Handles the Decommission phase.
    /// </summary>
    private async Task<StampPhase> HandleDecommissionPhaseAsync(StampConfigMap stamp, CancellationToken cancellationToken)
    {
        this._logger.LogInformation("Decommissioning stamp: {Id}", stamp.Id);

        // Simulate decommissioning logic
        await Task.Delay(this._options.DecommissionDelayMs, cancellationToken).ConfigureAwait(false);

        stamp.Status = "Decommissioned successfully";
        return StampPhase.Archive;
    }

    /// <summary>
    /// Handles the Archive phase.
    /// </summary>
    private async Task<StampPhase> HandleArchivePhaseAsync(StampConfigMap stamp, CancellationToken cancellationToken)
    {
        this._logger.LogInformation("Archiving stamp: {Id}", stamp.Id);

        // Simulate archiving logic
        await Task.Delay(this._options.ArchiveDelayMs, cancellationToken).ConfigureAwait(false);

        stamp.Status = "Archived successfully";
        return StampPhase.Purge;
    }

    /// <summary>
    /// Handles the Purge phase.
    /// </summary>
    private async Task<StampPhase> HandlePurgePhaseAsync(StampConfigMap stamp, CancellationToken cancellationToken)
    {
        this._logger.LogInformation("Purging stamp: {Id}", stamp.Id);

        // Simulate purging logic
        await Task.Delay(this._options.PurgeDelayMs, cancellationToken).ConfigureAwait(false);

        stamp.Status = "Purged successfully";
        return StampPhase.Purge; // Terminal state
    }

    /// <summary>
    /// Deserializes a stamp from a ConfigMap.
    /// </summary>
    private StampConfigMap? DeserializeStamp(V1ConfigMap configMap)
    {
        try
        {
            if (!configMap.Data.TryGetValue("stamp", out var stampJson))
            {
                return null;
            }

            var stamp = JsonSerializer.Deserialize<StampConfigMap>(stampJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

            return stamp;
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to deserialize stamp from ConfigMap: {Name}", configMap.Metadata.Name);
            return null;
        }
    }

    /// <summary>
    /// Updates a stamp in the ConfigMap.
    /// </summary>
    private Task UpdateStampAsync(StampConfigMap stamp, V1ConfigMap configMap, CancellationToken cancellationToken)
    {
        var stampJson = JsonSerializer.Serialize(stamp, new JsonSerializerOptions
        {
            WriteIndented = true,
        });

        configMap.Data["stamp"] = stampJson;

        // Update annotations with current state
        configMap.Metadata.Annotations ??= new Dictionary<string, string>(StringComparer.Ordinal);
        configMap.Metadata.Annotations["synaxis.io/phase"] = stamp.Phase.ToString();
        configMap.Metadata.Annotations["synaxis.io/status"] = stamp.Status;
        configMap.Metadata.Annotations["synaxis.io/last-updated"] = DateTime.UtcNow.ToString("o");

        return this._kubernetesClient.UpdateConfigMapAsync(configMap, cancellationToken);
    }
}
