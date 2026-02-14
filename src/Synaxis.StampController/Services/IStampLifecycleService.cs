// =============================================================================
// Stamp Lifecycle Service Interface
// =============================================================================

using Synaxis.StampController.Controllers;
using Synaxis.StampController.CRDs;

namespace Synaxis.StampController.Services;

/// <summary>
/// Service for managing stamp lifecycle operations
/// </summary>
public interface IStampLifecycleService
{
    /// <summary>
    /// Provisions a new stamp
    /// </summary>
    Task ProvisionStampAsync(StampResource stamp, CancellationToken cancellationToken);

    /// <summary>
    /// Checks the status of stamp provisioning
    /// </summary>
    Task<ProvisioningStatus> CheckProvisioningStatusAsync(StampResource stamp, CancellationToken cancellationToken);

    /// <summary>
    /// Evaluates if scaling is needed
    /// </summary>
    Task<ScalingEvaluation> EvaluateScalingAsync(StampResource stamp, StampMetrics metrics, CancellationToken cancellationToken);

    /// <summary>
    /// Executes scaling operations
    /// </summary>
    Task<OperationResult> ExecuteScalingAsync(StampResource stamp, CancellationToken cancellationToken);

    /// <summary>
    /// Attempts automatic recovery for a degraded stamp
    /// </summary>
    Task<OperationResult> AttemptRecoveryAsync(StampResource stamp, CancellationToken cancellationToken);

    /// <summary>
    /// Checks the status of stamp draining
    /// </summary>
    Task<DrainStatus> CheckDrainStatusAsync(StampResource stamp, CancellationToken cancellationToken);

    /// <summary>
    /// Decommissions a stamp
    /// </summary>
    Task<OperationResult> DecommissionStampAsync(StampResource stamp, CancellationToken cancellationToken);

    /// <summary>
    /// Terminates a stamp and cleans up resources
    /// </summary>
    Task TerminateStampAsync(StampResource stamp, CancellationToken cancellationToken);
}
