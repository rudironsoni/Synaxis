// <copyright file="ConversationTurn.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Core.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    /// <summary>
    /// Represents a single turn in a conversation.
    /// </summary>
    [Table("ConversationTurns")]
    public class ConversationTurn
    {
        /// <summary>
        /// Gets or sets the unique identifier for the conversation turn.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the conversation ID.
        /// </summary>
        [Required]
        public Guid ConversationId { get; set; }

        /// <summary>
        /// Gets or sets the turn number in the conversation.
        /// </summary>
        [Required]
        public int TurnNumber { get; set; }

        /// <summary>
        /// Gets or sets the role (user, assistant, system).
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the message content.
        /// </summary>
        [Required]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets optional metadata for the turn.
        /// </summary>
        [Column(TypeName = "jsonb")]
        public IDictionary<string, string>? Metadata { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the turn was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the conversation this turn belongs to.
        /// </summary>
        [ForeignKey(nameof(ConversationId))]
        public virtual Conversation? Conversation { get; set; }
    }
}
