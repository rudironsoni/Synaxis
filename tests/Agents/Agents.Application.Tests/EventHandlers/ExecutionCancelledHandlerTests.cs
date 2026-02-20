// <copyright file="ExecutionCancelledHandlerTests.cs" company="Synaxis">
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
/// Unit tests for <see cref="ExecutionCancelledHandler"/>.
/// </summary>
public class ExecutionCancelledHandlerTests
{
    private readonly Mock<ILogger<ExecutionCancelledHandler>> _loggerMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<IExecutionMetrics> _metricsMock;
    private readonly ExecutionCancelledHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionCancelledHandlerTests"/> class.
    /// </summary>
    public ExecutionCancelledHandlerTests()
    {
        this._loggerMock = new Mock<ILogger<ExecutionCancelledHandler>>();
        this._auditServiceMock = new Mock<IAuditService>();
        this._metricsMock = new Mock<IExecutionMetrics>();
        this._handler = new ExecutionCancelledHandler(
            this._loggerMock.Object,
            this._auditServiceMock.Object,
            this._metricsMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidEvent_LogsWarningWithExecutionIdAndDuration()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var notification = new ExecutionCancelled
        {
            Id = executionId,
            CurrentStep = 2,
            CancelledAt = DateTime.UtcNow,
            DurationMs = 500,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Execution cancelled") && v.ToString().Contains(executionId.ToString()) && v.ToString().Contains("500")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidEvent_CallsRecordExecutionCancelledAsync()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var notification = new ExecutionCancelled
        {
            Id = executionId,
            CurrentStep = 2,
            CancelledAt = DateTime.UtcNow,
            DurationMs = 500,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._metricsMock.Verify(
            x => x.RecordExecutionCancelledAsync(
                executionId,
                500,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidEvent_CallsAuditServiceLogEventAsync()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var notification = new ExecutionCancelled
        {
            Id = executionId,
            CurrentStep = 2,
            CancelledAt = DateTime.UtcNow,
            DurationMs = 500,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._auditServiceMock.Verify(
            x => x.LogEventAsync(It.Is<AuditEvent>(auditEvent =>
                auditEvent.EventType == nameof(ExecutionCancelled) &&
                auditEvent.EventCategory == "Execution" &&
                auditEvent.Action == "Cancel" &&
                auditEvent.ResourceType == "Execution" &&
                auditEvent.ResourceId == executionId.ToString())),
            Times.Once);
    }
}
