using System.ComponentModel.DataAnnotations;

namespace Synaxis.BatchProcessing.Models;

/// <summary>
/// Represents an individual item in a batch.
/// </summary>
public class BatchItem
{
    /// <summary>
    /// Gets or sets the unique identifier for the item.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the item data.
    /// </summary>
    [Required]
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the item status.
    /// </summary>
    public BatchItemStatus Status { get; set; } = BatchItemStatus.Pending;

    /// <summary>
    /// Gets or sets the item result.
    /// </summary>
    public string Result { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the error message if the item failed.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the processing timestamp.
    /// </summary>
    public DateTime ProcessedAt { get; set; }
}
