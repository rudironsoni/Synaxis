// <copyright file="ExecutionPausedHandlerTests.cs" company="Synaxis">
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
using Synaxis.Agents.Domain.Events;
using Synaxis.Core.Contracts;
using Xunit;

/// <summary>
/// Unit tests for <see cref="ExecutionPausedHandler"/>.
/// </summary>
public class ExecutionPausedHandlerTests
{
    private readonly Mock<ILogger<ExecutionPausedHandler>> _loggerMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly ExecutionPausedHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionPausedHandlerTests"/> class.
    /// </summary>
    public ExecutionPausedHandlerTests()
    {
        this._loggerMock = new Mock<ILogger<ExecutionPausedHandler>>();
        this._auditServiceMock = new Mock<IAuditService>();
        this._handler = new ExecutionPausedHandler(
            this._loggerMock.Object,
            this._auditServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidEvent_LogsInformationWithExecutionIdAndStep()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var notification = new ExecutionPaused
        {
            Id = executionId,
            CurrentStep = 3,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Execution paused") && v.ToString().Contains(executionId.ToString()) && v.ToString().Contains("3")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidEvent_CallsAuditServiceLogEventAsync()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var notification = new ExecutionPaused
        {
            Id = executionId,
            CurrentStep = 3,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._auditServiceMock.Verify(
            x => x.LogEventAsync(It.Is<AuditEvent>(auditEvent =>
                auditEvent.EventType == nameof(ExecutionPaused) &&
                auditEvent.EventCategory == "Execution" &&
                auditEvent.Action == "Pause" &&
                auditEvent.ResourceType == "Execution" &&
                auditEvent.ResourceId == executionId.ToString())),
            Times.Once);
    }
}
