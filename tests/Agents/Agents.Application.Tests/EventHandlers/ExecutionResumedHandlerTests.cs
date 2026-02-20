// <copyright file="ExecutionResumedHandlerTests.cs" company="Synaxis">
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
/// Unit tests for <see cref="ExecutionResumedHandler"/>.
/// </summary>
public class ExecutionResumedHandlerTests
{
    private readonly Mock<ILogger<ExecutionResumedHandler>> _loggerMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly ExecutionResumedHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionResumedHandlerTests"/> class.
    /// </summary>
    public ExecutionResumedHandlerTests()
    {
        this._loggerMock = new Mock<ILogger<ExecutionResumedHandler>>();
        this._auditServiceMock = new Mock<IAuditService>();
        this._handler = new ExecutionResumedHandler(
            this._loggerMock.Object,
            this._auditServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidEvent_LogsInformationWithExecutionIdAndStep()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var notification = new ExecutionResumed
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
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Execution resumed") && v.ToString().Contains(executionId.ToString()) && v.ToString().Contains("3")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidEvent_CallsAuditServiceLogEventAsync()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var notification = new ExecutionResumed
        {
            Id = executionId,
            CurrentStep = 3,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._auditServiceMock.Verify(
            x => x.LogEventAsync(It.Is<AuditEvent>(auditEvent =>
                auditEvent.EventType == nameof(ExecutionResumed) &&
                auditEvent.EventCategory == "Execution" &&
                auditEvent.Action == "Resume" &&
                auditEvent.ResourceType == "Execution" &&
                auditEvent.ResourceId == executionId.ToString())),
            Times.Once);
    }
}
