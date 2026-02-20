// <copyright file="AgentExecutionRepositoryTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.UnitTests.Repositories;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Synaxis.Abstractions.Cloud;
using Synaxis.Agents.Domain.Aggregates;
using Synaxis.Agents.Domain.Events;
using Synaxis.Agents.Infrastructure.Repositories;
using Xunit;

[Trait("Category", "Unit")]
public class AgentExecutionRepositoryTests
{
    private readonly Mock<IEventStore> _eventStoreMock;
    private readonly Mock<ILogger<AgentExecutionRepository>> _loggerMock;
    private readonly AgentExecutionRepository _repository;

    public AgentExecutionRepositoryTests()
    {
        _eventStoreMock = new Mock<IEventStore>();
        _loggerMock = new Mock<ILogger<AgentExecutionRepository>>();
        _repository = new AgentExecutionRepository(_eventStoreMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingStream_ReturnsAgentExecution()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var events = new List<IDomainEvent>
        {
            new ExecutionStarted
            {
                Id = executionId,
                AgentId = agentId,
                ExecutionId = "exec-123",
                InputParameters = new Dictionary<string, object>(),
                StartedAt = DateTime.UtcNow
            }
        };

        _eventStoreMock.Setup(x => x.ReadStreamAsync(executionId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(events);

        // Act
        var result = await _repository.GetByIdAsync(executionId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(executionId.ToString());
        result.AgentId.Should().Be(agentId);
        result.ExecutionId.Should().Be("exec-123");
        _eventStoreMock.Verify(x => x.ReadStreamAsync(executionId.ToString(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_EmptyStream_ReturnsNull()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        _eventStoreMock.Setup(x => x.ReadStreamAsync(executionId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IDomainEvent>());

        // Act
        var result = await _repository.GetByIdAsync(executionId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_CancellationToken_PassedToEventStore()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var cancellationToken = new CancellationToken(true);
        _eventStoreMock.Setup(x => x.ReadStreamAsync(executionId.ToString(), cancellationToken))
            .ReturnsAsync(new List<IDomainEvent>());

        // Act
        await _repository.GetByIdAsync(executionId, cancellationToken);

        // Assert
        _eventStoreMock.Verify(x => x.ReadStreamAsync(executionId.ToString(), cancellationToken), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WithUncommittedEvents_AppendsEventsToStore()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var uncommittedEvents = new List<IDomainEvent>
        {
            new ExecutionStarted
            {
                Id = executionId,
                AgentId = Guid.NewGuid(),
                ExecutionId = "exec-123",
                InputParameters = new Dictionary<string, object>(),
                StartedAt = DateTime.UtcNow
            }
        };

        var mockAggregate = CreateMockAggregate(executionId, uncommittedEvents, version: 1);

        _eventStoreMock.Setup(x => x.AppendAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<IDomainEvent>>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _repository.SaveAsync(mockAggregate);

        // Assert
        _eventStoreMock.Verify(x => x.AppendAsync(
            executionId.ToString(),
            It.Is<IEnumerable<IDomainEvent>>(e => e != null),
            0,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WithUncommittedEvents_CallsMarkAsCommitted()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var uncommittedEvents = new List<IDomainEvent>
        {
            new ExecutionStarted
            {
                Id = executionId,
                AgentId = Guid.NewGuid(),
                ExecutionId = "exec-123",
                InputParameters = new Dictionary<string, object>(),
                StartedAt = DateTime.UtcNow
            }
        };

        var mockAggregate = CreateMockAggregate(executionId, uncommittedEvents, version: 1);

        _eventStoreMock.Setup(x => x.AppendAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<IDomainEvent>>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _repository.SaveAsync(mockAggregate);

        // Assert
        mockAggregate.GetUncommittedEvents().Should().BeEmpty();
    }

    [Fact]
    public async Task SaveAsync_WithNoUncommittedEvents_DoesNotAppendToStore()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var mockAggregate = CreateMockAggregate(executionId, new List<IDomainEvent>(), version: 1);

        // Act
        await _repository.SaveAsync(mockAggregate);

        // Assert
        _eventStoreMock.Verify(x => x.AppendAsync(
            It.IsAny<string>(),
            It.IsAny<IEnumerable<IDomainEvent>>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_NullExecution_ThrowsArgumentNullException()
    {
        // Arrange
        AgentExecution? execution = null;

        // Act
        Func<Task> act = async () => await _repository.SaveAsync(execution!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetByAgentIdAsync_ReturnsEmptyList()
    {
        // Arrange
        var agentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByAgentIdAsync(agentId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRunningExecutionsAsync_ReturnsEmptyList()
    {
        // Act
        var result = await _repository.GetRunningExecutionsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_NullEventStore_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new AgentExecutionRepository(
            null!,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("eventStore");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new AgentExecutionRepository(
            _eventStoreMock.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    /// <summary>
    /// Creates a mock AgentExecution that returns specific uncommitted events for testing.
    /// </summary>
    private static AgentExecution CreateMockAggregate(Guid id, List<IDomainEvent> uncommittedEvents, int version)
    {
        var aggregate = new AgentExecution();

        // Use reflection to set the Id
        var idProperty = typeof(Synaxis.Infrastructure.EventSourcing.AggregateRoot).GetProperty("Id");
        idProperty!.SetValue(aggregate, id.ToString());

        // Add uncommitted events via reflection
        if (uncommittedEvents.Count > 0)
        {
            var uncommittedEventsField = typeof(Synaxis.Infrastructure.EventSourcing.AggregateRoot)
                .GetField("_uncommittedEvents", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var list = (List<IDomainEvent>)uncommittedEventsField!.GetValue(aggregate)!;
            list.AddRange(uncommittedEvents);
        }

        // Set version via reflection
        var versionProperty = typeof(Synaxis.Infrastructure.EventSourcing.AggregateRoot).GetProperty("Version");
        versionProperty!.SetValue(aggregate, version);

        return aggregate;
    }
}
