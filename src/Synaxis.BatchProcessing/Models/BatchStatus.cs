namespace Synaxis.BatchProcessing.Models;

/// <summary>
/// Represents the status of a batch.
/// </summary>
public enum BatchStatus
{
    /// <summary>
    /// The batch is pending processing.
    /// </summary>
    Pending,

    /// <summary>
    /// The batch is queued for processing.
    /// </summary>
    Queued,

    /// <summary>
    /// The batch is currently processing.
    /// </summary>
    Processing,

    /// <summary>
    /// The batch has completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// The batch has failed.
    /// </summary>
    Failed,

    /// <summary>
    /// The batch has been cancelled.
    /// </summary>
    Cancelled,

    /// <summary>
    /// The batch is retrying.
    /// </summary>
    Retrying
}
