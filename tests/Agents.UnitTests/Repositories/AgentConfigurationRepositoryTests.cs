// <copyright file="AgentConfigurationRepositoryTests.cs" company="Synaxis">
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
using Synaxis.Agents.Domain.ValueObjects;
using Synaxis.Agents.Infrastructure.Repositories;
using Xunit;

[Trait("Category", "Unit")]
public class AgentConfigurationRepositoryTests
{
    private readonly Mock<IEventStore> _eventStoreMock;
    private readonly Mock<ILogger<AgentConfigurationRepository>> _loggerMock;
    private readonly AgentConfigurationRepository _repository;

    public AgentConfigurationRepositoryTests()
    {
        _eventStoreMock = new Mock<IEventStore>();
        _loggerMock = new Mock<ILogger<AgentConfigurationRepository>>();
        _repository = new AgentConfigurationRepository(_eventStoreMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingStream_ReturnsAgentConfiguration()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var events = new List<IDomainEvent>
        {
            new AgentConfigurationCreated
            {
                AgentId = agentId,
                Name = "Test Agent",
                Description = "A test agent",
                AgentType = "declarative",
                ConfigurationYaml = "name: Test\nsteps: []",
                TenantId = tenantId
            }
        };

        _eventStoreMock.Setup(x => x.ReadStreamAsync(agentId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(events);

        // Act
        var result = await _repository.GetByIdAsync(agentId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(agentId);
        result.Name.Should().Be("Test Agent");
        _eventStoreMock.Verify(x => x.ReadStreamAsync(agentId.ToString(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_EmptyStream_ReturnsNull()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        _eventStoreMock.Setup(x => x.ReadStreamAsync(agentId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IDomainEvent>());

        // Act
        var result = await _repository.GetByIdAsync(agentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_CancellationToken_PassedToEventStore()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var cancellationToken = new CancellationToken(true);
        _eventStoreMock.Setup(x => x.ReadStreamAsync(agentId.ToString(), cancellationToken))
            .ReturnsAsync(new List<IDomainEvent>());

        // Act
        await _repository.GetByIdAsync(agentId, cancellationToken);

        // Assert
        _eventStoreMock.Verify(x => x.ReadStreamAsync(agentId.ToString(), cancellationToken), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WithUncommittedEvents_AppendsEventsToStore()
    {
        // Arrange - Create a mock aggregate that returns uncommitted events
        var agentId = Guid.NewGuid();
        var uncommittedEvents = new List<IDomainEvent>
        {
            new AgentConfigurationCreated { AgentId = agentId, Name = "Test", AgentType = "declarative", ConfigurationYaml = "test" }
        };

        var mockAggregate = CreateMockAggregate(agentId, uncommittedEvents, version: 1);

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
            agentId.ToString(),
            It.Is<IEnumerable<IDomainEvent>>(e => e != null),
            0,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WithMultipleUncommittedEvents_CalculatesCorrectExpectedVersion()
    {
        // Arrange - Create a mock aggregate with uncommitted events
        var agentId = Guid.NewGuid();
        var uncommittedEvents = new List<IDomainEvent>
        {
            new AgentConfigurationUpdated { AgentId = agentId, Name = "Updated", ConfigurationYaml = "updated", Version = 2, Timestamp = DateTime.UtcNow }
        };

        // Version is 1, with 1 uncommitted event, so expectedVersion should be 0 (1 - 1)
        var mockAggregate = CreateMockAggregate(agentId, uncommittedEvents, version: 1);

        _eventStoreMock.Setup(x => x.AppendAsync(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<IDomainEvent>>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _repository.SaveAsync(mockAggregate);

        // Assert - expectedVersion = version - uncommittedEvents.Count = 1 - 1 = 0
        _eventStoreMock.Verify(x => x.AppendAsync(
            agentId.ToString(),
            It.Is<IEnumerable<IDomainEvent>>(e => e != null),
            0,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WithNoUncommittedEvents_DoesNotAppendToStore()
    {
        // Arrange - Create a mock aggregate with no uncommitted events
        var agentId = Guid.NewGuid();
        var mockAggregate = CreateMockAggregate(agentId, new List<IDomainEvent>(), version: 1);

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
    public async Task SaveAsync_NullAgent_ThrowsArgumentNullException()
    {
        // Arrange
        AgentConfiguration? agent = null;

        // Act
        Func<Task> act = async () => await _repository.SaveAsync(agent!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SaveAsync_CallsMarkAsCommitted()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var uncommittedEvents = new List<IDomainEvent>
        {
            new AgentConfigurationCreated { AgentId = agentId, Name = "Test", AgentType = "declarative", ConfigurationYaml = "test" }
        };
        var mockAggregate = CreateMockAggregate(agentId, uncommittedEvents, version: 1);

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
    public async Task DeleteAsync_DeletesStreamFromStore()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        _eventStoreMock.Setup(x => x.DeleteAsync(agentId.ToString(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _repository.DeleteAsync(agentId);

        // Assert
        _eventStoreMock.Verify(x => x.DeleteAsync(agentId.ToString(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_CancellationToken_PassedToEventStore()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var cancellationToken = new CancellationToken(true);
        _eventStoreMock.Setup(x => x.DeleteAsync(agentId.ToString(), cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        await _repository.DeleteAsync(agentId, cancellationToken);

        // Assert
        _eventStoreMock.Verify(x => x.DeleteAsync(agentId.ToString(), cancellationToken), Times.Once);
    }

    [Fact]
    public async Task GetByTenantAsync_ReturnsEmptyList()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByTenantAsync(tenantId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByStatusAsync_ReturnsEmptyList()
    {
        // Arrange
        var status = AgentStatus.Active;

        // Act
        var result = await _repository.GetByStatusAsync(status);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_NullEventStore_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new AgentConfigurationRepository(
            null!,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("eventStore");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new AgentConfigurationRepository(
            _eventStoreMock.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    /// <summary>
    /// Creates a mock AgentConfiguration that returns specific uncommitted events for testing.
    /// </summary>
    private static AgentConfiguration CreateMockAggregate(Guid id, List<IDomainEvent> uncommittedEvents, int version)
    {
        // Create the aggregate and use reflection to set up the state
        var aggregate = AgentConfiguration.Create(
            id,
            "Test Agent",
            null,
            "declarative",
            "name: Test\nsteps: []",
            Guid.NewGuid(),
            null,
            null);

        // Clear the uncommitted events and set up the desired state via reflection
        aggregate.MarkAsCommitted();

        // Use reflection to set the Version property via its private setter
        var versionProperty = typeof(Synaxis.Infrastructure.EventSourcing.AggregateRoot)
            .GetProperty("Version", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        versionProperty!.SetValue(aggregate, version);

        // Add uncommitted events via reflection (they're in a private list)
        if (uncommittedEvents.Count > 0)
        {
            var uncommittedEventsField = typeof(Synaxis.Infrastructure.EventSourcing.AggregateRoot)
                .GetField("_uncommittedEvents", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var list = (List<IDomainEvent>)uncommittedEventsField!.GetValue(aggregate)!;
            list.AddRange(uncommittedEvents);
        }

        return aggregate;
    }
}
