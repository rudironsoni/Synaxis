// <copyright file="OutboxProcessorTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.IntegrationTests;

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Synaxis.Abstractions.Cloud;
using Synaxis.Infrastructure.Messaging;
using Xunit;

/// <summary>
/// Integration tests for Outbox Processor.
/// </summary>
public sealed class OutboxProcessorTests : IAsyncLifetime
{
    private readonly IOutbox _outbox;
    private readonly TestDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxProcessorTests"/> class.
    /// </summary>
    public OutboxProcessorTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new TestDbContext(options);

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<SqlOutbox>();

        _outbox = new SqlOutbox(_context, logger);
    }

    /// <summary>
    /// Test DbContext with OutboxMessage entity.
    /// </summary>
    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options)
            : base(options)
        {
        }

        public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EventType).IsRequired();
                entity.Property(e => e.Payload).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.ProcessedAt);
                entity.Property(e => e.Error);
            });
        }
    }

    /// <inheritdoc />
    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task Should_Save_And_Retrieve_Outbox_Message()
    {
        // Arrange
        var @event = new TestEvent { Data = "test", Value = 42 };

        // Act
        await _outbox.SaveAsync(@event);
        await _context.SaveChangesAsync();
        var messages = await _outbox.GetUnprocessedAsync();

        // Assert
        messages.Should().HaveCount(1);
        messages[0].EventType.Should().Be("TestEvent");
    }

    [Fact]
    public async Task Should_Save_Multiple_Messages()
    {
        // Arrange
        var events = new[]
        {
            new TestEvent { Data = "event1", Value = 1 },
            new TestEvent { Data = "event2", Value = 2 },
            new TestEvent { Data = "event3", Value = 3 }
        };

        // Act
        foreach (var @event in events)
        {
            await _outbox.SaveAsync(@event);
        }

        await _context.SaveChangesAsync();
        var messages = await _outbox.GetUnprocessedAsync();

        // Assert
        messages.Should().HaveCount(3);
    }

    [Fact]
    public async Task Should_Mark_Message_As_Processed()
    {
        // Arrange
        var @event = new TestEvent { Data = "test", Value = 42 };
        await _outbox.SaveAsync(@event);
        await _context.SaveChangesAsync();
        var messages = await _outbox.GetUnprocessedAsync();
        var messageId = messages[0].Id;

        // Act
        await _outbox.MarkAsProcessedAsync(messageId);
        await _context.SaveChangesAsync();
        var remainingMessages = await _outbox.GetUnprocessedAsync();

        // Assert
        remainingMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Mark_Message_As_Failed()
    {
        // Arrange
        var @event = new TestEvent { Data = "test", Value = 42 };
        await _outbox.SaveAsync(@event);
        await _context.SaveChangesAsync();
        var messages = await _outbox.GetUnprocessedAsync();
        var messageId = messages[0].Id;

        // Act
        await _outbox.MarkAsFailedAsync(messageId, "Processing failed");
        await _context.SaveChangesAsync();
        var remainingMessages = await _outbox.GetUnprocessedAsync();

        // Assert
        remainingMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Retrieve_Batch_Of_Messages()
    {
        // Arrange
        for (int i = 0; i < 15; i++)
        {
            await _outbox.SaveAsync(new TestEvent { Data = $"event{i}", Value = i });
        }

        await _context.SaveChangesAsync();

        // Act
        var batch1 = await _outbox.GetUnprocessedAsync(10);
        var batch2 = await _outbox.GetUnprocessedAsync(10);

        // Assert
        batch1.Should().HaveCount(10);
        batch2.Should().HaveCount(5);
    }

    [Fact]
    public async Task Should_Handle_Different_Event_Types()
    {
        // Arrange
        var events = new IDomainEvent[]
        {
            new TestEvent { Data = "test", Value = 42 },
            new AnotherTestEvent { Message = "another", Timestamp = DateTime.UtcNow.Ticks }
        };

        foreach (var @event in events)
        {
            await _outbox.SaveAsync(@event);
        }

        await _context.SaveChangesAsync();

        // Act
        var messages = await _outbox.GetUnprocessedAsync();

        // Assert
        messages.Should().HaveCount(2);
        messages[0].EventType.Should().Be("TestEvent");
        messages[1].EventType.Should().Be("AnotherTestEvent");
    }

    [Fact]
    public async Task Should_Preserve_Event_Payload()
    {
        // Arrange
        var @event = new TestEvent { Data = "test data", Value = 123 };
        await _outbox.SaveAsync(@event);
        await _context.SaveChangesAsync();

        // Act
        var messages = await _outbox.GetUnprocessedAsync();
        var payload = System.Text.Json.JsonSerializer.Deserialize<TestEvent>(messages[0].Payload);

        // Assert
        payload.Should().NotBeNull();
        payload!.Data.Should().Be("test data");
        payload.Value.Should().Be(123);
    }

    [Fact]
    public async Task Should_Handle_Empty_Outbox()
    {
        // Act
        var messages = await _outbox.GetUnprocessedAsync();

        // Assert
        messages.Should().BeEmpty();
    }
}
