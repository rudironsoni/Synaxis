namespace Synaxis.BatchProcessing.Models;

/// <summary>
/// Represents the status of a batch item.
/// </summary>
public enum BatchItemStatus
{
    /// <summary>
    /// The item is pending processing.
    /// </summary>
    Pending,

    /// <summary>
    /// The item is currently processing.
    /// </summary>
    Processing,

    /// <summary>
    /// The item has completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// The item has failed.
    /// </summary>
    Failed
}
