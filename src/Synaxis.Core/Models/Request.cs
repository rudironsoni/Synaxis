using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Synaxis.Core.Models
{
    /// <summary>
    /// API request with full audit trail and cross-border tracking
    /// </summary>
    public class Request
    {
        public Guid Id { get; set; }
        
        /// <summary>
        /// Global unique request ID
        /// </summary>
        public Guid RequestId { get; set; }
        
        public Guid OrganizationId { get; set; }
        
        public virtual Organization Organization { get; set; }
        
        public Guid? UserId { get; set; }
        
        public virtual User User { get; set; }
        
        public Guid? VirtualKeyId { get; set; }
        
        public virtual VirtualKey VirtualKey { get; set; }
        
        public Guid? TeamId { get; set; }
        
        public virtual Team Team { get; set; }
        
        /// <summary>
        /// Region where user data is stored
        /// </summary>
        public string UserRegion { get; set; }
        
        /// <summary>
        /// Region where request was processed
        /// </summary>
        public string ProcessedRegion { get; set; }
        
        /// <summary>
        /// Region where data is persisted
        /// </summary>
        public string StoredRegion { get; set; }
        
        /// <summary>
        /// Was this a cross-border transfer
        /// </summary>
        public bool CrossBorderTransfer { get; set; }
        
        /// <summary>
        /// Legal basis for transfer: SCC, consent, adequacy, none
        /// </summary>
        public string TransferLegalBasis { get; set; }
        
        /// <summary>
        /// Purpose of cross-border transfer
        /// </summary>
        public string TransferPurpose { get; set; }
        
        public DateTime? TransferTimestamp { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Model { get; set; }
        
        [StringLength(100)]
        public string Provider { get; set; }
        
        public int InputTokens { get; set; }
        
        public int OutputTokens { get; set; }
        
        public int TotalTokens => InputTokens + OutputTokens;
        
        /// <summary>
        /// Cost in USD
        /// </summary>
        public decimal Cost { get; set; }
        
        /// <summary>
        /// Request duration in milliseconds
        /// </summary>
        public int DurationMs { get; set; }
        
        /// <summary>
        /// Time spent in queue
        /// </summary>
        public int QueueTimeMs { get; set; }
        
        public int RequestSizeBytes { get; set; }
        
        public int ResponseSizeBytes { get; set; }
        
        public int StatusCode { get; set; }
        
        public string ClientIpAddress { get; set; }
        
        public string UserAgent { get; set; }
        
        public Dictionary<string, string> RequestHeaders { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? CompletedAt { get; set; }
    }
}
