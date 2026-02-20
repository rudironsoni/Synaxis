// <copyright file="ExecutionCompletedHandlerTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.Tests.EventHandlers;

using System;
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
/// Unit tests for <see cref="ExecutionCompletedHandler"/>.
/// </summary>
public class ExecutionCompletedHandlerTests
{
    private readonly Mock<ILogger<ExecutionCompletedHandler>> _loggerMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<IExecutionMetrics> _metricsMock;
    private readonly ExecutionCompletedHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionCompletedHandlerTests"/> class.
    /// </summary>
    public ExecutionCompletedHandlerTests()
    {
        this._loggerMock = new Mock<ILogger<ExecutionCompletedHandler>>();
        this._auditServiceMock = new Mock<IAuditService>();
        this._metricsMock = new Mock<IExecutionMetrics>();
        this._handler = new ExecutionCompletedHandler(
            this._loggerMock.Object,
            this._auditServiceMock.Object,
            this._metricsMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidEvent_LogsInformationWithExecutionIdAndDuration()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var notification = new ExecutionCompleted
        {
            Id = executionId,
            CompletedAt = DateTime.UtcNow,
            DurationMs = 1500,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Execution completed") && v.ToString().Contains(executionId.ToString()) && v.ToString().Contains("1500")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidEvent_CallsRecordExecutionCompletedAsync()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var notification = new ExecutionCompleted
        {
            Id = executionId,
            CompletedAt = DateTime.UtcNow,
            DurationMs = 1500,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._metricsMock.Verify(
            x => x.RecordExecutionCompletedAsync(
                executionId,
                1500,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidEvent_CallsAuditServiceLogEventAsync()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var notification = new ExecutionCompleted
        {
            Id = executionId,
            CompletedAt = DateTime.UtcNow,
            DurationMs = 1500,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._auditServiceMock.Verify(
            x => x.LogEventAsync(It.Is<AuditEvent>(auditEvent =>
                auditEvent.EventType == nameof(ExecutionCompleted) &&
                auditEvent.EventCategory == "Execution" &&
                auditEvent.Action == "Complete" &&
                auditEvent.ResourceType == "Execution" &&
                auditEvent.ResourceId == executionId.ToString())),
            Times.Once);
    }
}
