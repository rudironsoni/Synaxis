using System.ComponentModel.DataAnnotations;

namespace Synaxis.BatchProcessing.Controllers;

/// <summary>
/// Request model for creating a batch.
/// </summary>
public class CreateBatchRequest
{
    /// <summary>
    /// Gets or sets the batch name.
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the batch description.
    /// </summary>
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of batch operation.
    /// </summary>
    [Required]
    [StringLength(50)]
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the items to process in the batch.
    /// </summary>
    [Required]
    public List<BatchItem> Items { get; set; } = new List<BatchItem>();

    /// <summary>
    /// Gets or sets the webhook URL for completion notifications.
    /// </summary>
    [Url]
    public string WebhookUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the priority of the batch.
    /// </summary>
    public BatchPriority Priority { get; set; } = BatchPriority.Normal;
}
