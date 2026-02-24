// <copyright file="WorkflowFailedHandlerTests.cs" company="Synaxis">
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
/// Unit tests for <see cref="WorkflowFailedHandler"/>.
/// </summary>
public class WorkflowFailedHandlerTests
{
    private readonly Mock<ILogger<WorkflowFailedHandler>> _loggerMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly WorkflowFailedHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowFailedHandlerTests"/> class.
    /// </summary>
    public WorkflowFailedHandlerTests()
    {
        this._loggerMock = new Mock<ILogger<WorkflowFailedHandler>>();
        this._auditServiceMock = new Mock<IAuditService>();
        this._handler = new WorkflowFailedHandler(
            this._loggerMock.Object,
            this._auditServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidEvent_LogsErrorWithWorkflowDetails()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var notification = new WorkflowFailed
        {
            Id = workflowId,
            StepNumber = 2,
            Error = "Connection timeout",
            FailedAt = DateTime.UtcNow,
            Timestamp = DateTimeOffset.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Workflow failed") && v.ToString().Contains(workflowId.ToString()) && v.ToString().Contains("2") && v.ToString().Contains("Connection timeout")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidEvent_LogsWarningForHighPriorityAlert()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var notification = new WorkflowFailed
        {
            Id = workflowId,
            StepNumber = 2,
            Error = "Connection timeout",
            FailedAt = DateTime.UtcNow,
            Timestamp = DateTimeOffset.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("HIGH PRIORITY ALERT") && v.ToString().Contains(workflowId.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidEvent_CallsAuditServiceLogEventAsync()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var notification = new WorkflowFailed
        {
            Id = workflowId,
            StepNumber = 2,
            Error = "Connection timeout",
            FailedAt = DateTime.UtcNow,
            Timestamp = DateTimeOffset.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._auditServiceMock.Verify(
            x => x.LogEventAsync(It.Is<AuditEvent>(auditEvent =>
                auditEvent.EventType == "Workflow.Failed" &&
                auditEvent.EventCategory == "Workflow" &&
                auditEvent.Action == "Fail" &&
                auditEvent.ResourceType == "Workflow" &&
                auditEvent.ResourceId == workflowId.ToString())),
            Times.Once);
    }
}
