// <copyright file="Snapshot.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.EventSourcing.Models;

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Database entity representing a snapshot of an aggregate state.
/// </summary>
[Table("event_store_snapshots")]
public sealed class Snapshot
{
    /// <summary>
    /// Gets or sets the unique identifier for this snapshot.
    /// </summary>
    [Key]
    [Column("id")]
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the stream identifier this snapshot belongs to.
    /// </summary>
    [Column("stream_id")]
    [Required]
    [MaxLength(512)]
    public string StreamId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the version of the stream at the time of this snapshot.
    /// </summary>
    [Column("version")]
    public long Version { get; set; }

    /// <summary>
    /// Gets or sets the type name of the aggregate.
    /// </summary>
    [Column("aggregate_type")]
    [Required]
    [MaxLength(512)]
    public string AggregateType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the serialized aggregate state in JSON format.
    /// </summary>
    [Column("state_data", TypeName = "jsonb")]
    [Required]
    public string StateData { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the snapshot was created.
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the number of events applied since last snapshot.
    /// </summary>
    [Column("event_count")]
    public int EventCount { get; set; }
}
