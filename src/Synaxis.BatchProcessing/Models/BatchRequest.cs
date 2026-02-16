// <copyright file="BatchRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.BatchProcessing.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Represents a batch processing request.
    /// </summary>
    public class BatchRequest
    {
        /// <summary>
        /// Gets or sets the unique identifier for the batch.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the organization identifier.
        /// </summary>
        [Required]
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the user identifier who submitted the batch.
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

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
        /// Gets or sets the batch status.
        /// </summary>
        public BatchStatus Status { get; set; } = BatchStatus.Pending;

        /// <summary>
        /// Gets or sets the total number of items in the batch.
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// Gets or sets the number of items processed.
        /// </summary>
        public int ProcessedItems { get; set; }

        /// <summary>
        /// Gets or sets the number of items that failed.
        /// </summary>
        public int FailedItems { get; set; }

        /// <summary>
        /// Gets the progress percentage (0-100).
        /// </summary>
        public int ProgressPercentage => this.TotalItems > 0 ? (int)((double)this.ProcessedItems / this.TotalItems * 100) : 0;

        /// <summary>
        /// Gets or sets the webhook URL for completion notifications.
        /// </summary>
        [Url]
        public string WebhookUrl { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the start timestamp.
        /// </summary>
        public DateTime StartedAt { get; set; }

        /// <summary>
        /// Gets or sets the completion timestamp.
        /// </summary>
        public DateTime CompletedAt { get; set; }

        /// <summary>
        /// Gets or sets the error message if the batch failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the result blob storage path.
        /// </summary>
        public string ResultBlobPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the retry count.
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// Gets or sets the maximum retry count.
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Gets or sets the priority of the batch.
        /// </summary>
        public BatchPriority Priority { get; set; } = BatchPriority.Normal;

        /// <summary>
        /// Gets or sets the metadata associated with the batch.
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}
