// <copyright file="EventRecord.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.EventSourcing.Models;

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Database entity representing a stored event in the event store.
/// </summary>
[Table("event_store")]
public sealed class EventRecord
{
    /// <summary>
    /// Gets or sets the unique identifier for this event record.
    /// </summary>
    [Key]
    [Column("id")]
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the global position of this event across all streams.
    /// </summary>
    [Column("global_position")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long GlobalPosition { get; set; }

    /// <summary>
    /// Gets or sets the stream identifier this event belongs to.
    /// </summary>
    [Column("stream_id")]
    [Required]
    [MaxLength(512)]
    public string StreamId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version of this event within its stream.
    /// </summary>
    [Column("version")]
    public long Version { get; set; }

    /// <summary>
    /// Gets or sets the type name of the event.
    /// </summary>
    [Column("event_type")]
    [Required]
    [MaxLength(512)]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the serialized event data in JSON format.
    /// </summary>
    [Column("event_data", TypeName = "jsonb")]
    [Required]
    public string EventData { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the serialized metadata in JSON format.
    /// </summary>
    [Column("metadata", TypeName = "jsonb")]
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the event was recorded.
    /// </summary>
    [Column("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the unique event identifier.
    /// </summary>
    [Column("event_id")]
    [Required]
    public Guid EventId { get; set; }
}
