// <copyright file="Request.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// API request with full audit trail and cross-border tracking.
    /// </summary>
    public class Request
    {
        /// <summary>
        /// Gets or sets the unique identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the global unique request identifier.
        /// </summary>
        public Guid RequestId { get; set; }

        /// <summary>
        /// Gets or sets the organization identifier.
        /// </summary>
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the organization navigation property.
        /// </summary>
        public virtual Organization? Organization { get; set; }

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the user navigation property.
        /// </summary>
        public virtual User? User { get; set; }

        /// <summary>
        /// Gets or sets the virtual key identifier.
        /// </summary>
        public Guid? VirtualKeyId { get; set; }

        /// <summary>
        /// Gets or sets the virtual key navigation property.
        /// </summary>
        public virtual VirtualKey? VirtualKey { get; set; }

        /// <summary>
        /// Gets or sets the team identifier.
        /// </summary>
        public Guid? TeamId { get; set; }

        /// <summary>
        /// Gets or sets the team navigation property.
        /// </summary>
        public virtual Team? Team { get; set; }

        /// <summary>
        /// Gets or sets the region where user data is stored.
        /// </summary>
        public string UserRegion { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the region where request was processed.
        /// </summary>
        public string ProcessedRegion { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the region where data is persisted.
        /// </summary>
        public string StoredRegion { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether this was a cross-border transfer.
        /// </summary>
        public bool CrossBorderTransfer { get; set; }

        /// <summary>
        /// Gets or sets the legal basis for transfer: SCC, consent, adequacy, none.
        /// </summary>
        public string TransferLegalBasis { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the purpose of cross-border transfer.
        /// </summary>
        public string TransferPurpose { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the transfer timestamp.
        /// </summary>
        public DateTime? TransferTimestamp { get; set; }

        /// <summary>
        /// Gets or sets the model name.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Model { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the provider name.
        /// </summary>
        [StringLength(100)]
        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of input tokens.
        /// </summary>
        public int InputTokens { get; set; }

        /// <summary>
        /// Gets or sets the number of output tokens.
        /// </summary>
        public int OutputTokens { get; set; }

        /// <summary>
        /// Gets the total number of tokens.
        /// </summary>
        public int TotalTokens => this.InputTokens + this.OutputTokens;

        /// <summary>
        /// Gets or sets the cost in USD.
        /// </summary>
        public decimal Cost { get; set; }

        /// <summary>
        /// Gets or sets the request duration in milliseconds.
        /// </summary>
        public int DurationMs { get; set; }

        /// <summary>
        /// Gets or sets the time spent in queue.
        /// </summary>
        public int QueueTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the request size in bytes.
        /// </summary>
        public int RequestSizeBytes { get; set; }

        /// <summary>
        /// Gets or sets the response size in bytes.
        /// </summary>
        public int ResponseSizeBytes { get; set; }

        /// <summary>
        /// Gets or sets the HTTP status code.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the client IP address.
        /// </summary>
        public string ClientIpAddress { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the user agent string.
        /// </summary>
        public string UserAgent { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the HTTP request headers.
        /// </summary>
        public IDictionary<string, string> RequestHeaders { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the completion timestamp.
        /// </summary>
        public DateTime? CompletedAt { get; set; }
    }
}
