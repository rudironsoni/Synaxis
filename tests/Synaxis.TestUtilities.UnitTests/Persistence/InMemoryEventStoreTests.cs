// <copyright file="InMemoryEventStoreTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.TestUtilities.Tests.Persistence;

using FluentAssertions;
using Synaxis.Abstractions.Cloud;
using Synaxis.TestUtilities.Persistence;
using Xunit;

/// <summary>
/// Unit tests for <see cref="InMemoryEventStore"/>.
/// </summary>
public class InMemoryEventStoreTests
{
    private readonly InMemoryEventStore _store = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryEventStoreTests"/> class.
    /// </summary>
    public InMemoryEventStoreTests()
    {
        _store.Clear();
    }

    /// <summary>
    /// Tests that AppendAsync throws ArgumentException when streamId is null or empty.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task AppendAsync_WithInvalidStreamId_ThrowsArgumentException(string? streamId)
    {
        // Arrange
        var events = new[] { new TestDomainEvent(Guid.NewGuid().ToString()) };

        // Act
        var act = async () => await _store.AppendAsync(streamId!, events, 0);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    /// <summary>
    /// Tests that AppendAsync throws ArgumentNullException when events is null.
    /// </summary>
    [Fact]
    public async Task AppendAsync_WithNullEvents_ThrowsArgumentNullException()
    {
        // Act
        var act = async () => await _store.AppendAsync("stream-1", null!, 0);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that AppendAsync succeeds with valid arguments.
    /// </summary>
    [Fact]
    public async Task AppendAsync_WithValidArguments_Succeeds()
    {
        // Arrange
        var streamId = "stream-1";
        var events = new[] { new TestDomainEvent(Guid.NewGuid().ToString()) };

        // Act
        await _store.AppendAsync(streamId, events, 0);

        // Assert
        _store.StreamExists(streamId).Should().BeTrue();
        _store.GetEventCount().Should().Be(1);
    }

    /// <summary>
    /// Tests that AppendAsync enforces optimistic concurrency.
    /// </summary>
    [Fact]
    public async Task AppendAsync_WithWrongExpectedVersion_ThrowsInvalidOperationException()
    {
        // Arrange
        var streamId = "stream-1";
        var events = new[] { new TestDomainEvent(Guid.NewGuid().ToString()) };
        await _store.AppendAsync(streamId, events, 0);

        // Act
        var act = async () => await _store.AppendAsync(streamId, events, 0);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    /// <summary>
    /// Tests that AppendAsync appends events with correct expected version.
    /// </summary>
    [Fact]
    public async Task AppendAsync_WithCorrectExpectedVersion_Succeeds()
    {
        // Arrange
        var streamId = "stream-1";
        var event1 = new TestDomainEvent(Guid.NewGuid().ToString());
        var event2 = new TestDomainEvent(Guid.NewGuid().ToString());
        await _store.AppendAsync(streamId, new[] { event1 }, 0);

        // Act
        await _store.AppendAsync(streamId, new[] { event2 }, 1);

        // Assert
        var storedEvents = await _store.ReadStreamAsync(streamId);
        storedEvents.Should().HaveCount(2);
    }

    /// <summary>
    /// Tests that ReadStreamAsync returns events for existing stream.
    /// </summary>
    [Fact]
    public async Task ReadStreamAsync_WithExistingStream_ReturnsEvents()
    {
        // Arrange
        var streamId = "stream-1";
        var events = new[] { new TestDomainEvent(Guid.NewGuid().ToString()) };
        await _store.AppendAsync(streamId, events, 0);

        // Act
        var result = await _store.ReadStreamAsync(streamId);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Be(events[0]);
    }

    /// <summary>
    /// Tests that ReadStreamAsync returns empty list for non-existent stream.
    /// </summary>
    [Fact]
    public async Task ReadStreamAsync_WithNonExistentStream_ReturnsEmptyList()
    {
        // Act
        var result = await _store.ReadStreamAsync("non-existent");

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that ReadStreamAsync throws ArgumentException for null streamId.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ReadStreamAsync_WithInvalidStreamId_ThrowsArgumentException(string? streamId)
    {
        // Act
        var act = async () => await _store.ReadStreamAsync(streamId!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    /// <summary>
    /// Tests that ReadAsync returns events within version range.
    /// </summary>
    [Fact]
    public async Task ReadAsync_WithVersionRange_ReturnsEventsInRange()
    {
        // Arrange
        var streamId = "stream-1";
        var events = new List<TestDomainEvent>();
        for (var i = 0; i < 5; i++)
        {
            events.Add(new TestDomainEvent(Guid.NewGuid().ToString()));
        }

        await _store.AppendAsync(streamId, events, 0);

        // Act
        var result = await _store.ReadAsync(streamId, 1, 3);

        // Assert
        result.Should().HaveCount(3);
    }

    /// <summary>
    /// Tests that DeleteAsync removes the stream.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_WithExistingStream_RemovesStream()
    {
        // Arrange
        var streamId = "stream-1";
        await _store.AppendAsync(streamId, new[] { new TestDomainEvent(Guid.NewGuid().ToString()) }, 0);

        // Act
        await _store.DeleteAsync(streamId);

        // Assert
        _store.StreamExists(streamId).Should().BeFalse();
    }

    /// <summary>
    /// Tests that DeleteAsync throws ArgumentException for null streamId.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task DeleteAsync_WithInvalidStreamId_ThrowsArgumentException(string? streamId)
    {
        // Act
        var act = async () => await _store.DeleteAsync(streamId!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    /// <summary>
    /// Tests that ReadAllAsync returns all events across all streams.
    /// </summary>
    [Fact]
    public async Task ReadAllAsync_ReturnsAllEvents()
    {
        // Arrange
        await _store.AppendAsync("stream-1", new[] { new TestDomainEvent(Guid.NewGuid().ToString()) }, 0);
        await _store.AppendAsync("stream-2", new[] { new TestDomainEvent(Guid.NewGuid().ToString()) }, 0);

        // Act
        var result = await _store.ReadAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    /// <summary>
    /// Tests that SubscribeToAllAsync notifies subscribers of new events.
    /// </summary>
    [Fact]
    public async Task SubscribeToAllAsync_NotifiesSubscribersOfNewEvents()
    {
        // Arrange
        var receivedEvents = new List<IDomainEvent>();
        var tcs = new TaskCompletionSource<bool>();

        await _store.SubscribeToAllAsync(async evt =>
        {
            receivedEvents.Add(evt);
            if (receivedEvents.Count == 1)
            {
                tcs.SetResult(true);
            }
        });

        var @event = new TestDomainEvent(Guid.NewGuid().ToString());

        // Act
        await _store.AppendAsync("stream-1", new[] { @event }, 0);

        // Wait for subscriber to be notified
        await tcs.Task;

        // Assert
        receivedEvents.Should().HaveCount(1);
    }

    /// <summary>
    /// Tests that SubscribeToAllAsync throws ArgumentNullException for null handler.
    /// </summary>
    [Fact]
    public async Task SubscribeToAllAsync_WithNullHandler_ThrowsArgumentNullException()
    {
        // Act
        var act = async () => await _store.SubscribeToAllAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that Clear removes all events and subscribers.
    /// </summary>
    [Fact]
    public async Task Clear_RemovesAllEventsAndSubscribers()
    {
        // Arrange
        await _store.AppendAsync("stream-1", new[] { new TestDomainEvent(Guid.NewGuid().ToString()) }, 0);
        await _store.SubscribeToAllAsync(_ => Task.CompletedTask);

        // Act
        _store.Clear();

        // Assert
        _store.GetEventCount().Should().Be(0);
        (await _store.ReadAllAsync()).Should().BeEmpty();
    }

    /// <summary>
    /// Tests that GetStreamIds returns all stream identifiers.
    /// </summary>
    [Fact]
    public async Task GetStreamIds_ReturnsAllStreamIds()
    {
        // Arrange
        await _store.AppendAsync("stream-1", new[] { new TestDomainEvent(Guid.NewGuid().ToString()) }, 0);
        await _store.AppendAsync("stream-2", new[] { new TestDomainEvent(Guid.NewGuid().ToString()) }, 0);

        // Act
        var result = _store.GetStreamIds();

        // Assert
        result.Should().Contain("stream-1");
        result.Should().Contain("stream-2");
    }

    /// <summary>
    /// Tests that multiple threads can append events to different streams concurrently.
    /// </summary>
    [Fact]
    public async Task AppendAsync_ConcurrentAppendsToDifferentStreams_Succeeds()
    {
        // Arrange
        const int threadCount = 10;
        var tasks = new List<Task>();
        var successCount = 0;
        var lockObj = new object();

        // Act
        for (var i = 0; i < threadCount; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var streamId = $"stream-{index}";
                    var @event = new TestDomainEvent(Guid.NewGuid().ToString());
                    await _store.AppendAsync(streamId, new[] { @event }, 0);
                    lock (lockObj)
                    {
                        successCount++;
                    }
                }
                catch
                {
                    // Ignore - some threads may fail due to timing, but that's OK for this test
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - at least one event should have been added
        successCount.Should().Be(threadCount);
        _store.GetEventCount().Should().Be(threadCount);
    }

    /// <summary>
    /// Tests that GetEventCount returns the correct count.
    /// </summary>
    [Fact]
    public async Task GetEventCount_AfterAppends_ReturnsCorrectCount()
    {
        // Arrange
        await _store.AppendAsync("stream-1", new[] { new TestDomainEvent(Guid.NewGuid().ToString()) }, 0);
        await _store.AppendAsync("stream-2", new[] { new TestDomainEvent(Guid.NewGuid().ToString()) }, 0);

        // Act
        var count = _store.GetEventCount();

        // Assert
        count.Should().Be(2);
    }

    /// <summary>
    /// Tests that StreamExists returns correct values.
    /// </summary>
    [Fact]
    public async Task StreamExists_WithExistingAndNonExistingStreams_ReturnsCorrectly()
    {
        // Arrange
        await _store.AppendAsync("existing-stream", new[] { new TestDomainEvent(Guid.NewGuid().ToString()) }, 0);

        // Act & Assert
        _store.StreamExists("existing-stream").Should().BeTrue();
        _store.StreamExists("non-existing-stream").Should().BeFalse();
    }

    /// <summary>
    /// Test domain event implementation.
    /// </summary>
    private sealed class TestDomainEvent : Synaxis.Infrastructure.EventSourcing.DomainEvent
    {
        public TestDomainEvent(string aggregateId)
        {
            AggregateId = aggregateId;
        }

        public override string AggregateId { get; }
    }
}
