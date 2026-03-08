// <copyright file="EventStoreDbContext.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Shared.Kernel.Shared.Kernel.Infrastructure.EventSourcing;

using Microsoft.EntityFrameworkCore;
using Synaxis.Shared.Kernel.Shared.Kernel.Infrastructure.EventSourcing.Models;

/// <summary>
/// Database context for the event store.
/// </summary>
public class EventStoreDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public EventStoreDbContext(DbContextOptions<EventStoreDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the event records.
    /// </summary>
    public DbSet<EventRecord> Events => this.Set<EventRecord>();

    /// <summary>
    /// Gets or sets the snapshots.
    /// </summary>
    public DbSet<Snapshot> Snapshots => this.Set<Snapshot>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureEventRecord(modelBuilder);
        ConfigureSnapshot(modelBuilder);
    }

    private static void ConfigureEventRecord(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EventRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.GlobalPosition)
                .HasColumnName("global_position")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.StreamId)
                .HasColumnName("stream_id")
                .IsRequired()
                .HasMaxLength(512);

            entity.Property(e => e.Version)
                .HasColumnName("version");

            entity.Property(e => e.EventType)
                .HasColumnName("event_type")
                .IsRequired()
                .HasMaxLength(512);

            entity.Property(e => e.EventData)
                .HasColumnName("event_data")
                .HasColumnType("jsonb")
                .IsRequired();

            entity.Property(e => e.Metadata)
                .HasColumnName("metadata")
                .HasColumnType("jsonb");

            entity.Property(e => e.Timestamp)
                .HasColumnName("timestamp");

            entity.Property(e => e.EventId)
                .HasColumnName("event_id")
                .IsRequired();

            // Indexes
            entity.HasIndex(e => new { e.StreamId, e.Version })
                .IsUnique()
                .HasDatabaseName("ix_event_store_stream_version");

            entity.HasIndex(e => e.GlobalPosition)
                .IsUnique()
                .HasDatabaseName("ix_event_store_global_position");

            entity.HasIndex(e => e.EventType)
                .HasDatabaseName("ix_event_store_event_type");

            entity.HasIndex(e => e.Timestamp)
                .HasDatabaseName("ix_event_store_timestamp");

            entity.ToTable("event_store");
        });
    }

    private static void ConfigureSnapshot(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Snapshot>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.StreamId)
                .HasColumnName("stream_id")
                .IsRequired()
                .HasMaxLength(512);

            entity.Property(e => e.Version)
                .HasColumnName("version");

            entity.Property(e => e.AggregateType)
                .HasColumnName("aggregate_type")
                .IsRequired()
                .HasMaxLength(512);

            entity.Property(e => e.StateData)
                .HasColumnName("state_data")
                .HasColumnType("jsonb")
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at");

            entity.Property(e => e.EventCount)
                .HasColumnName("event_count");

            // Indexes
            entity.HasIndex(e => e.StreamId)
                .IsUnique()
                .HasDatabaseName("ix_snapshots_stream_id");

            entity.HasIndex(e => e.AggregateType)
                .HasDatabaseName("ix_snapshots_aggregate_type");

            entity.ToTable("event_store_snapshots");
        });
    }
}
