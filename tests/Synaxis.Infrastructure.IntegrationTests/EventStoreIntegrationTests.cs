// <copyright file="EventStoreIntegrationTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.IntegrationTests;

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Npgsql;
using Synaxis.Abstractions.Cloud;
using Synaxis.Providers.OnPrem;
using Xunit;

/// <summary>
/// Integration tests for Event Store using PostgreSQL with TestContainers.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "Infrastructure")]
public sealed class EventStoreIntegrationTests : IClassFixture<PostgreSqlTestFixture>, IAsyncLifetime
{
    private readonly PostgreSqlTestFixture _fixture;
    private PostgreSqlEventStore? _eventStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreIntegrationTests"/> class.
    /// </summary>
    /// <param name="fixture">The PostgreSQL fixture.</param>
    public EventStoreIntegrationTests(PostgreSqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    /// <inheritdoc />
    public Task InitializeAsync()
    {
        var logger = _fixture.LoggerFactory.CreateLogger<PostgreSqlEventStore>();
        _eventStore = new PostgreSqlEventStore(_fixture.ConnectionString, logger);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        await _fixture.ClearDataAsync();
    }

    [Fact]
    public async Task AppendAsync_AppendsEventsToDatabase()
    {
        // Arrange
        var streamId = Guid.NewGuid().ToString();
        var @event = new TestEvent { Data = "test", Value = 42 };

        // Act
        await _eventStore!.AppendAsync(streamId, new[] { @event }, 0);
        var events = await _eventStore.ReadStreamAsync(streamId);

        // Assert
        events.Should().HaveCount(1);
        events[0].EventType.Should().Be("TestEvent");
    }

    [Fact]
    public async Task ReadStreamAsync_ReturnsEventsInOrder()
    {
        // Arrange
        var streamId = Guid.NewGuid().ToString();
        var events = new[]
        {
            new TestEvent { Data = "event1", Value = 1 },
            new TestEvent { Data = "event2", Value = 2 },
            new TestEvent { Data = "event3", Value = 3 }
        };

        // Act
        await _eventStore!.AppendAsync(streamId, events, 0);
        var readEvents = await _eventStore.ReadStreamAsync(streamId);

        // Assert
        readEvents.Should().HaveCount(3);
        ((TestEvent)readEvents[0]).Data.Should().Be("event1");
        ((TestEvent)readEvents[1]).Data.Should().Be("event2");
        ((TestEvent)readEvents[2]).Data.Should().Be("event3");
    }

    [Fact]
    public async Task ReadAsync_ReturnsEventsByVersionRange()
    {
        // Arrange
        var streamId = Guid.NewGuid().ToString();
        var events = new[]
        {
            new TestEvent { Data = "event1", Value = 1 },
            new TestEvent { Data = "event2", Value = 2 },
            new TestEvent { Data = "event3", Value = 3 },
            new TestEvent { Data = "event4", Value = 4 },
            new TestEvent { Data = "event5", Value = 5 }
        };

        await _eventStore!.AppendAsync(streamId, events, 0);

        // Act
        var readEvents = await _eventStore.ReadAsync(streamId, 2, 4);

        // Assert
        readEvents.Should().HaveCount(3);
    }

    [Fact]
    public async Task AppendAsync_HandlesOptimisticConcurrency()
    {
        // Arrange
        var streamId = Guid.NewGuid().ToString();
        var event1 = new TestEvent { Data = "event1", Value = 1 };
        var event2 = new TestEvent { Data = "event2", Value = 2 };

        await _eventStore!.AppendAsync(streamId, new[] { event1 }, 0);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _eventStore.AppendAsync(streamId, new[] { event2 }, 0));

        exception.Message.Should().Contain("Concurrency conflict");
    }

    [Fact]
    public async Task DeleteAsync_RemovesStream()
    {
        // Arrange
        var streamId = Guid.NewGuid().ToString();
        var @event = new TestEvent { Data = "test", Value = 42 };

        await _eventStore!.AppendAsync(streamId, new[] { @event }, 0);

        // Act
        await _eventStore.DeleteAsync(streamId);
        var events = await _eventStore.ReadStreamAsync(streamId);

        // Assert
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task AppendAsync_HandlesMultipleEventTypes()
    {
        // Arrange
        var streamId = Guid.NewGuid().ToString();
        var events = new IDomainEvent[]
        {
            new TestEvent { Data = "test", Value = 42 },
            new AnotherTestEvent { Message = "another", Timestamp = DateTime.UtcNow.Ticks }
        };

        // Act
        await _eventStore!.AppendAsync(streamId, events, 0);
        var readEvents = await _eventStore.ReadStreamAsync(streamId);

        // Assert
        readEvents.Should().HaveCount(2);
        readEvents.Select(e => e.EventType).Should().Contain("TestEvent");
        readEvents.Select(e => e.EventType).Should().Contain("AnotherTestEvent");
    }

    [Fact]
    public async Task ReadStreamAsync_HandlesEmptyStream()
    {
        // Arrange
        var streamId = Guid.NewGuid().ToString();

        // Act
        var events = await _eventStore!.ReadStreamAsync(streamId);

        // Assert
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task AppendAsync_MaintainsCorrectVersioning()
    {
        // Arrange
        var streamId = Guid.NewGuid().ToString();

        // Act
        await _eventStore!.AppendAsync(streamId, new[] { new TestEvent { Data = "event1", Value = 1 } }, 0);
        await _eventStore.AppendAsync(streamId, new[] { new TestEvent { Data = "event2", Value = 2 } }, 1);
        await _eventStore.AppendAsync(streamId, new[] { new TestEvent { Data = "event3", Value = 3 } }, 2);

        var events = await _eventStore.ReadStreamAsync(streamId);

        // Assert
        events.Should().HaveCount(3);
    }

    [Fact]
    public async Task AppendAsync_PreservesEventData()
    {
        // Arrange
        var streamId = Guid.NewGuid().ToString();
        var @event = new TestEvent
        {
            Data = "Complex data with special chars: !@#$%^&*()",
            Value = int.MaxValue
        };

        // Act
        await _eventStore!.AppendAsync(streamId, new[] { @event }, 0);
        var events = await _eventStore.ReadStreamAsync(streamId);

        // Assert
        events.Should().HaveCount(1);
        var retrievedEvent = (TestEvent)events[0];
        retrievedEvent.Data.Should().Be(@event.Data);
        retrievedEvent.Value.Should().Be(@event.Value);
    }

    [Fact]
    public async Task AppendAsync_HandlesMultipleStreams()
    {
        // Arrange
        var streamId1 = Guid.NewGuid().ToString();
        var streamId2 = Guid.NewGuid().ToString();

        // Act
        await _eventStore!.AppendAsync(streamId1, new[] { new TestEvent { Data = "stream1", Value = 1 } }, 0);
        await _eventStore.AppendAsync(streamId2, new[] { new TestEvent { Data = "stream2", Value = 2 } }, 0);

        var events1 = await _eventStore.ReadStreamAsync(streamId1);
        var events2 = await _eventStore.ReadStreamAsync(streamId2);

        // Assert
        events1.Should().HaveCount(1);
        events2.Should().HaveCount(1);
        ((TestEvent)events1[0]).Data.Should().Be("stream1");
        ((TestEvent)events2[0]).Data.Should().Be("stream2");
    }

    [Fact]
    public async Task AppendAsync_HandlesUnicodeData()
    {
        // Arrange
        var streamId = Guid.NewGuid().ToString();
        var @event = new TestEvent
        {
            Data = "Unicode: 你好世界 🌍 Привет мир",
            Value = 42
        };

        // Act
        await _eventStore!.AppendAsync(streamId, new[] { @event }, 0);
        var events = await _eventStore.ReadStreamAsync(streamId);

        // Assert
        events.Should().HaveCount(1);
        ((TestEvent)events[0]).Data.Should().Be(@event.Data);
    }

    [Fact]
    public async Task AppendAsync_HandlesLargeData()
    {
        // Arrange
        var streamId = Guid.NewGuid().ToString();
        var largeData = new string('X', 10000);
        var @event = new TestEvent { Data = largeData, Value = 42 };

        // Act
        await _eventStore!.AppendAsync(streamId, new[] { @event }, 0);
        var events = await _eventStore.ReadStreamAsync(streamId);

        // Assert
        events.Should().HaveCount(1);
        ((TestEvent)events[0]).Data.Should().HaveLength(10000);
    }

    [Fact]
    public async Task AppendAsync_HandlesConcurrentAppendsToDifferentStreams()
    {
        // Arrange
        var tasks = Enumerable.Range(0, 10).Select(async i =>
        {
            var streamId = $"stream-{i}";
            var @event = new TestEvent { Data = $"event-{i}", Value = i };
            await _eventStore!.AppendAsync(streamId, new[] { @event }, 0);
        });

        // Act
        await Task.WhenAll(tasks);

        // Assert
        for (int i = 0; i < 10; i++)
        {
            var events = await _eventStore!.ReadStreamAsync($"stream-{i}");
            events.Should().HaveCount(1);
        }
    }

    [Fact]
    public async Task DeleteAsync_IsIdempotent()
    {
        // Arrange
        var streamId = Guid.NewGuid().ToString();

        // Act
        await _eventStore!.DeleteAsync(streamId);
        await _eventStore.DeleteAsync(streamId);

        // Assert
        var events = await _eventStore.ReadStreamAsync(streamId);
        events.Should().BeEmpty();
    }
}
