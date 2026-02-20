// <copyright file="AgentWorkflowRepositoryTests.cs" company="Synaxis">
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
public class AgentWorkflowRepositoryTests
{
    private readonly Mock<IEventStore> _eventStoreMock;
    private readonly Mock<ILogger<AgentWorkflowRepository>> _loggerMock;
    private readonly AgentWorkflowRepository _repository;

    public AgentWorkflowRepositoryTests()
    {
        _eventStoreMock = new Mock<IEventStore>();
        _loggerMock = new Mock<ILogger<AgentWorkflowRepository>>();
        _repository = new AgentWorkflowRepository(_eventStoreMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingStream_ReturnsAgentWorkflow()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var events = new List<IDomainEvent>
        {
            new WorkflowCreated
            {
                Id = workflowId,
                Name = "Test Workflow",
                Description = "A test workflow",
                WorkflowYaml = "name: Test\nsteps: []",
                TenantId = Guid.NewGuid().ToString(),
                TeamId = null,
                Version = 1
            }
        };

        _eventStoreMock.Setup(x => x.ReadStreamAsync(workflowId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(events);

        // Act
        var result = await _repository.GetByIdAsync(workflowId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(workflowId.ToString());
        result.Name.Should().Be("Test Workflow");
        result.WorkflowYaml.Should().Be("name: Test\nsteps: []");
        _eventStoreMock.Verify(x => x.ReadStreamAsync(workflowId.ToString(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_EmptyStream_ReturnsNull()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        _eventStoreMock.Setup(x => x.ReadStreamAsync(workflowId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IDomainEvent>());

        // Act
        var result = await _repository.GetByIdAsync(workflowId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_CancellationToken_PassedToEventStore()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var cancellationToken = new CancellationToken(true);
        _eventStoreMock.Setup(x => x.ReadStreamAsync(workflowId.ToString(), cancellationToken))
            .ReturnsAsync(new List<IDomainEvent>());

        // Act
        await _repository.GetByIdAsync(workflowId, cancellationToken);

        // Assert
        _eventStoreMock.Verify(x => x.ReadStreamAsync(workflowId.ToString(), cancellationToken), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WithUncommittedEvents_AppendsEventsToStore()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var uncommittedEvents = new List<IDomainEvent>
        {
            new WorkflowCreated
            {
                Id = workflowId,
                Name = "Test Workflow",
                Description = "A test workflow",
                WorkflowYaml = "name: Test\nsteps: []",
                TenantId = Guid.NewGuid().ToString(),
                TeamId = null,
                Version = 1
            }
        };

        var mockAggregate = CreateMockAggregate(workflowId, uncommittedEvents, version: 1);

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
            workflowId.ToString(),
            It.Is<IEnumerable<IDomainEvent>>(e => e != null),
            0,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WithUncommittedEvents_CallsMarkAsCommitted()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var uncommittedEvents = new List<IDomainEvent>
        {
            new WorkflowCreated
            {
                Id = workflowId,
                Name = "Test Workflow",
                WorkflowYaml = "test",
                TenantId = Guid.NewGuid().ToString(),
                Version = 1
            }
        };

        var mockAggregate = CreateMockAggregate(workflowId, uncommittedEvents, version: 1);

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
        var workflowId = Guid.NewGuid();
        var mockAggregate = CreateMockAggregate(workflowId, new List<IDomainEvent>(), version: 1);

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
    public async Task SaveAsync_NullWorkflow_ThrowsArgumentNullException()
    {
        // Arrange
        AgentWorkflow? workflow = null;

        // Act
        Func<Task> act = async () => await _repository.SaveAsync(workflow!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
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
    public void Constructor_NullEventStore_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new AgentWorkflowRepository(
            null!,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("eventStore");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new AgentWorkflowRepository(
            _eventStoreMock.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    /// <summary>
    /// Creates a mock AgentWorkflow that returns specific uncommitted events for testing.
    /// </summary>
    private static AgentWorkflow CreateMockAggregate(Guid id, List<IDomainEvent> uncommittedEvents, int version)
    {
        var aggregate = new AgentWorkflow();

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
