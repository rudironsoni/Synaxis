// <copyright file="Conversation.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    /// <summary>
    /// Represents a conversation between a user and the AI system.
    /// </summary>
    [Table("Conversations")]
    public class Conversation
    {
        /// <summary>
        /// Gets or sets the unique identifier for the conversation.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the user ID who owns the conversation.
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the organization ID.
        /// </summary>
        [Required]
        public Guid OrganizationId { get; set; }

        /// <summary>
        /// Gets or sets the conversation title.
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date and time when the conversation was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the conversation was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the conversation is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the collection of conversation turns.
        /// </summary>
        public virtual ICollection<ConversationTurn> Turns { get; set; } = new List<ConversationTurn>();

        /// <summary>
        /// Gets or sets the user navigation property.
        /// </summary>
        public virtual User? User { get; set; }

        /// <summary>
        /// Gets or sets the organization navigation property.
        /// </summary>
        public virtual Organization? Organization { get; set; }
    }
}
