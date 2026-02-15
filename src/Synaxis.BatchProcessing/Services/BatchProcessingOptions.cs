namespace Synaxis.BatchProcessing.Services;

/// <summary>
/// Configuration options for batch processing.
/// </summary>
public class BatchProcessingOptions
{
    /// <summary>
    /// Gets or sets the maximum number of concurrent batches.
    /// </summary>
    public int MaxConcurrentBatches { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum retry count.
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// Gets or sets the retry delay in seconds.
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 60;
}
