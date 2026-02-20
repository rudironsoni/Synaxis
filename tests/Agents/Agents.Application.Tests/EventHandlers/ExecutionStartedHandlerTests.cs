// <copyright file="ExecutionStartedHandlerTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.Tests.EventHandlers;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Synaxis.Agents.Application.EventHandlers;
using Synaxis.Agents.Application.Interfaces;
using Synaxis.Agents.Domain.Events;
using Synaxis.Core.Contracts;
using Xunit;

/// <summary>
/// Unit tests for <see cref="ExecutionStartedHandler"/>.
/// </summary>
public class ExecutionStartedHandlerTests
{
    private readonly Mock<ILogger<ExecutionStartedHandler>> _loggerMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<IExecutionMetrics> _metricsMock;
    private readonly ExecutionStartedHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionStartedHandlerTests"/> class.
    /// </summary>
    public ExecutionStartedHandlerTests()
    {
        this._loggerMock = new Mock<ILogger<ExecutionStartedHandler>>();
        this._auditServiceMock = new Mock<IAuditService>();
        this._metricsMock = new Mock<IExecutionMetrics>();
        this._handler = new ExecutionStartedHandler(
            this._loggerMock.Object,
            this._auditServiceMock.Object,
            this._metricsMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidEvent_LogsInformationWithExecutionAndAgentId()
    {
        // Arrange
        var id = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var notification = new ExecutionStarted
        {
            Id = id,
            AgentId = agentId,
            ExecutionId = "exec-123",
            InputParameters = new Dictionary<string, object>(),
            StartedAt = DateTime.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Execution started") && v.ToString().Contains("exec-123") && v.ToString().Contains(agentId.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidEvent_CallsRecordExecutionStartedAsync()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var notification = new ExecutionStarted
        {
            Id = executionId,
            AgentId = agentId,
            ExecutionId = "exec-123",
            InputParameters = new Dictionary<string, object>(),
            StartedAt = DateTime.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._metricsMock.Verify(
            x => x.RecordExecutionStartedAsync(
                executionId,
                agentId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidEvent_CallsAuditServiceLogEventAsync()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var notification = new ExecutionStarted
        {
            Id = executionId,
            AgentId = agentId,
            ExecutionId = "exec-123",
            InputParameters = new Dictionary<string, object>(),
            StartedAt = DateTime.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._auditServiceMock.Verify(
            x => x.LogEventAsync(It.Is<AuditEvent>(auditEvent =>
                auditEvent.EventType == nameof(ExecutionStarted) &&
                auditEvent.EventCategory == "Execution" &&
                auditEvent.Action == "Start" &&
                auditEvent.ResourceType == "Execution" &&
                auditEvent.ResourceId == executionId.ToString())),
            Times.Once);
    }
}
