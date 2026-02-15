// <copyright file="AzureSqlEventStoreTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Infrastructure.IntegrationTests;

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Synaxis.Abstractions.Cloud;
using Xunit;

/// <summary>
/// Integration tests for Azure SQL Event Store.
/// </summary>
public sealed class AzureSqlEventStoreTests : IClassFixture<SqlServerFixture>
{
    private readonly IEventStore _eventStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureSqlEventStoreTests"/> class.
    /// </summary>
    /// <param name="fixture">The SQL Server fixture.</param>
    public AzureSqlEventStoreTests(SqlServerFixture fixture)
    {
        _eventStore = fixture.EventStore;
    }

    [Fact]
    public async Task Should_Append_And_Read_Events()
    {
        // Arrange
        var streamId = Guid.NewGuid().ToString();
        var @event = new TestEvent { Data = "test", Value = 42 };

        // Act
        await _eventStore.AppendAsync(streamId, new[] { @event }, 0);
        var events = await _eventStore.ReadStreamAsync(streamId);

        // Assert
        events.Should().HaveCount(1);
        events[0].EventType.Should().Be("TestEvent");
    }

    [Fact]
    public async Task Should_Append_Multiple_Events()
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
        await _eventStore.AppendAsync(streamId, events, 0);
        var readEvents = await _eventStore.ReadStreamAsync(streamId);

        // Assert
        readEvents.Should().HaveCount(3);
    }

    [Fact]
    public async Task Should_Read_Events_By_Version_Range()
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

        await _eventStore.AppendAsync(streamId, events, 0);

        // Act
        var readEvents = await _eventStore.ReadAsync(streamId, 2, 4);

        // Assert
        readEvents.Should().HaveCount(3);
    }

    [Fact]
    public async Task Should_Throw_On_Concurrency_Conflict()
    {
        // Arrange
        var streamId = Guid.NewGuid().ToString();
        var event1 = new TestEvent { Data = "event1", Value = 1 };
        var event2 = new TestEvent { Data = "event2", Value = 2 };

        await _eventStore.AppendAsync(streamId, new[] { event1 }, 0);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _eventStore.AppendAsync(streamId, new[] { event2 }, 0));

        exception.Message.Should().Contain("Concurrency conflict");
    }

    [Fact]
    public async Task Should_Delete_Stream()
    {
        // Arrange
        var streamId = Guid.NewGuid().ToString();
        var @event = new TestEvent { Data = "test", Value = 42 };

        await _eventStore.AppendAsync(streamId, new[] { @event }, 0);

        // Act
        await _eventStore.DeleteAsync(streamId);
        var events = await _eventStore.ReadStreamAsync(streamId);

        // Assert
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Handle_Empty_Stream()
    {
        // Arrange
        var streamId = Guid.NewGuid().ToString();

        // Act
        var events = await _eventStore.ReadStreamAsync(streamId);

        // Assert
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Append_Events_With_Correct_Versioning()
    {
        // Arrange
        var streamId = Guid.NewGuid().ToString();

        // Act
        await _eventStore.AppendAsync(streamId, new[] { new TestEvent { Data = "event1", Value = 1 } }, 0);
        await _eventStore.AppendAsync(streamId, new[] { new TestEvent { Data = "event2", Value = 2 } }, 1);
        await _eventStore.AppendAsync(streamId, new[] { new TestEvent { Data = "event3", Value = 3 } }, 2);

        var events = await _eventStore.ReadStreamAsync(streamId);

        // Assert
        events.Should().HaveCount(3);
    }

    [Fact]
    public async Task Should_Read_From_Non_Existent_Stream()
    {
        // Arrange
        var streamId = Guid.NewGuid().ToString();

        // Act
        var events = await _eventStore.ReadStreamAsync(streamId);

        // Assert
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Handle_Different_Event_Types()
    {
        // Arrange
        var streamId = Guid.NewGuid().ToString();
        var events = new IDomainEvent[]
        {
            new TestEvent { Data = "test", Value = 42 },
            new AnotherTestEvent { Message = "another", Timestamp = DateTime.UtcNow.Ticks }
        };

        // Act
        await _eventStore.AppendAsync(streamId, events, 0);
        var readEvents = await _eventStore.ReadStreamAsync(streamId);

        // Assert
        readEvents.Should().HaveCount(2);
        readEvents[0].EventType.Should().Be("TestEvent");
        readEvents[1].EventType.Should().Be("AnotherTestEvent");
    }
}
