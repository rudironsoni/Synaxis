// <copyright file="ExecutionFailedHandlerTests.cs" company="Synaxis">
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
using Synaxis.Core.Models;
using Xunit;

/// <summary>
/// Unit tests for <see cref="ExecutionFailedHandler"/>.
/// </summary>
public class ExecutionFailedHandlerTests
{
    private readonly Mock<ILogger<ExecutionFailedHandler>> _loggerMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<IExecutionMetrics> _metricsMock;
    private readonly ExecutionFailedHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionFailedHandlerTests"/> class.
    /// </summary>
    public ExecutionFailedHandlerTests()
    {
        this._loggerMock = new Mock<ILogger<ExecutionFailedHandler>>();
        this._auditServiceMock = new Mock<IAuditService>();
        this._metricsMock = new Mock<IExecutionMetrics>();
        this._handler = new ExecutionFailedHandler(
            this._loggerMock.Object,
            this._auditServiceMock.Object,
            this._metricsMock.Object);
    }

    [Fact]
    public async Task Handle_WithFailedExecution_LogsErrorAndCreatesAuditEntry()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var notification = new ExecutionFailed
        {
            Id = executionId,
            Error = "Test error message",
            FailedAt = DateTime.UtcNow,
            DurationMs = 5000,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Execution failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        this._auditServiceMock.Verify(
            x => x.LogEventAsync(It.IsAny<AuditEvent>()),
            Times.Once);

        this._metricsMock.Verify(
            x => x.RecordExecutionFailedAsync(
                executionId,
                "Test error message",
                5000,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
