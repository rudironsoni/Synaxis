// <copyright file="OutboxIntegrationTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.IntegrationTests;

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Synaxis.Abstractions.Cloud;
using Synaxis.Abstractions.Time;
using Synaxis.Common.Tests.Time;
using Synaxis.Infrastructure.Messaging;
using Xunit;

/// <summary>
/// Integration tests for Outbox using PostgreSQL with TestContainers.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "Infrastructure")]
public sealed class OutboxIntegrationTests : IClassFixture<PostgreSqlTestFixture>, IAsyncLifetime
{
    private readonly PostgreSqlTestFixture _fixture;
    private TestDbContext? _context;
    private SqlOutbox? _outbox;
    private TestTimeProvider? _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxIntegrationTests"/> class.
    /// </summary>
    /// <param name="fixture">The PostgreSQL fixture.</param>
    public OutboxIntegrationTests(PostgreSqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;

        _context = new TestDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        _timeProvider = new TestTimeProvider();
        var logger = _fixture.LoggerFactory.CreateLogger<SqlOutbox>();
        _outbox = new SqlOutbox(_context, logger, _timeProvider);
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        await _fixture.ClearDataAsync();
        await _context!.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task OutboxPattern_WorksCorrectly()
    {
        // Arrange
        var @event = new TestEvent { Data = "test", Value = 42 };

        // Act
        await _outbox!.SaveAsync(@event);
        await _context!.SaveChangesAsync();
        var messages = await _outbox.GetUnprocessedAsync();

        // Assert
        messages.Should().HaveCount(1);
        messages[0].EventType.Should().Be("Synaxis.Infrastructure.IntegrationTests.TestEvent");
        messages[0].ProcessedAt.Should().BeNull();
    }

    [Fact]
    public async Task Messages_ProcessedAsynchronously()
    {
        // Arrange
        var events = new[]
        {
            new TestEvent { Data = "event1", Value = 1 },
            new TestEvent { Data = "event2", Value = 2 },
            new TestEvent { Data = "event3", Value = 3 }
        };

        foreach (var @event in events)
        {
            await _outbox!.SaveAsync(@event);
        }

        await _context!.SaveChangesAsync();

        // Act - Simulate processing
        var messages = await _outbox!.GetUnprocessedAsync();
        foreach (var message in messages)
        {
            await _outbox.MarkAsProcessedAsync(message.Id);
        }

        await _context.SaveChangesAsync();
        var remainingMessages = await _outbox.GetUnprocessedAsync();

        // Assert
        remainingMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task RetryLogic_ForFailures()
    {
        // Arrange
        var @event = new TestEvent { Data = "test", Value = 42 };
        await _outbox!.SaveAsync(@event);
        await _context!.SaveChangesAsync();

        var messages = await _outbox.GetUnprocessedAsync();
        var messageId = messages[0].Id;

        // Act - Mark as failed
        await _outbox.MarkAsFailedAsync(messageId, "Processing failed");
        await _context.SaveChangesAsync();

        var remainingMessages = await _outbox.GetUnprocessedAsync();
        var allMessages = await _context.OutboxMessages.ToListAsync();

        // Assert - Failed messages remain in unprocessed for retry (outbox pattern)
        remainingMessages.Should().ContainSingle();
        allMessages.Should().ContainSingle();
        allMessages[0].Error.Should().Be("Processing failed");
        allMessages[0].RetryCount.Should().Be(1);
    }

    [Fact]
    public async Task Idempotency_MultipleSavesSameEvent()
    {
        // Arrange
        var @event = new TestEvent { Data = "test", Value = 42 };

        // Act
        await _outbox!.SaveAsync(@event);
        await _outbox.SaveAsync(@event); // Same event
        await _context!.SaveChangesAsync();

        var messages = await _outbox.GetUnprocessedAsync();

        // Assert - Both saves should create separate records (idempotency handled at application level)
        messages.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUnprocessedAsync_RetrievesBatch()
    {
        // Arrange
        for (int i = 0; i < 15; i++)
        {
            await _outbox!.SaveAsync(new TestEvent { Data = $"event{i}", Value = i });
        }

        await _context!.SaveChangesAsync();

        // Act
        var batch1 = await _outbox!.GetUnprocessedAsync(10);

        // Mark batch1 as processed to get remaining messages
        foreach (var message in batch1)
        {
            await _outbox.MarkAsProcessedAsync(message.Id);
        }

        await _context.SaveChangesAsync();
        var batch2 = await _outbox.GetUnprocessedAsync(10);

        // Assert
        batch1.Should().HaveCount(10);
        batch2.Should().HaveCount(5);
    }

    [Fact]
    public async Task Preserve_EventPayload()
    {
        // Arrange
        var @event = new TestEvent { Data = "test data", Value = 123 };
        await _outbox!.SaveAsync(@event);
        await _context!.SaveChangesAsync();

        // Act
        var messages = await _outbox.GetUnprocessedAsync();
        var payload = System.Text.Json.JsonSerializer.Deserialize<TestEvent>(messages[0].Payload);

        // Assert
        payload.Should().NotBeNull();
        payload!.Data.Should().Be("test data");
        payload.Value.Should().Be(123);
    }

    [Fact]
    public async Task Handle_DifferentEventTypes()
    {
        // Arrange
        var events = new IDomainEvent[]
        {
            new TestEvent { Data = "test", Value = 42 },
            new AnotherTestEvent { Message = "another", Timestamp = DateTime.UtcNow.Ticks }
        };

        foreach (var @event in events)
        {
            await _outbox!.SaveAsync(@event);
        }

        await _context!.SaveChangesAsync();

        // Act
        var messages = await _outbox!.GetUnprocessedAsync();

        // Assert
        messages.Should().HaveCount(2);
        messages.Select(m => m.EventType).Should().Contain("Synaxis.Infrastructure.IntegrationTests.TestEvent");
        messages.Select(m => m.EventType).Should().Contain("Synaxis.Infrastructure.IntegrationTests.AnotherTestEvent");
    }

    [Fact]
    public async Task Handle_EmptyOutbox()
    {
        // Act
        var messages = await _outbox!.GetUnprocessedAsync();

        // Assert
        messages.Should().BeEmpty();
    }

    [Fact]
    public async Task MarkAsProcessedAsync_UpdatesTimestamp()
    {
        // Arrange
        var @event = new TestEvent { Data = "test", Value = 42 };
        await _outbox!.SaveAsync(@event);
        await _context!.SaveChangesAsync();

        var beforeProcessing = _timeProvider!.UtcNow;
        var messages = await _outbox.GetUnprocessedAsync();
        var messageId = messages[0].Id;

        // Act
        await _timeProvider.Delay(TimeSpan.FromMilliseconds(100)); // Instant time advancement
        await _outbox.MarkAsProcessedAsync(messageId);
        await _context.SaveChangesAsync();

        // Assert
        var processedMessage = await _context.OutboxMessages.FindAsync(messageId);
        processedMessage.Should().NotBeNull();
        processedMessage!.ProcessedAt.Should().NotBeNull();
        processedMessage.ProcessedAt.Should().BeOnOrAfter(beforeProcessing);
    }

    [Fact]
    public async Task GetUnprocessedAsync_OrdersByCreatedAt()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            await _outbox!.SaveAsync(new TestEvent { Data = $"event{i}", Value = i });
            await _timeProvider!.Delay(TimeSpan.FromMilliseconds(10)); // Instant time advancement for ordering
        }

        await _context!.SaveChangesAsync();

        // Act
        var messages = await _outbox!.GetUnprocessedAsync();

        // Assert
        messages.Should().HaveCount(5);
        for (int i = 1; i < messages.Count; i++)
        {
            messages[i].CreatedAt.Should().BeOnOrAfter(messages[i - 1].CreatedAt);
        }
    }

    [Fact]
    public async Task Concurrent_SaveOperations()
    {
        // Arrange
        var tasks = Enumerable.Range(0, 10)
            .Select(async i =>
            {
                var @event = new TestEvent { Data = $"concurrent-{i}", Value = i };
                await _outbox!.SaveAsync(@event);
            });

        // Act
        await Task.WhenAll(tasks);
        await _context!.SaveChangesAsync();

        // Assert
        var messages = await _outbox!.GetUnprocessedAsync();
        messages.Should().HaveCount(10);
    }

    /// <summary>
    /// Test DbContext with OutboxMessage entity.
    /// </summary>
    private class TestDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestDbContext"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        public TestDbContext(DbContextOptions<TestDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Gets or sets the outbox messages.
        /// </summary>
        public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.ToTable("outbox_messages");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.EventType).HasColumnName("event_type").IsRequired();
                entity.Property(e => e.Payload).HasColumnName("payload").IsRequired();
                entity.Property(e => e.Headers).HasColumnName("headers");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
                entity.Property(e => e.ProcessedAt).HasColumnName("processed_at");
                entity.Property(e => e.Error).HasColumnName("error");
                entity.Property(e => e.RetryCount).HasColumnName("retry_count").HasDefaultValue(0);
            });
        }
    }
}
