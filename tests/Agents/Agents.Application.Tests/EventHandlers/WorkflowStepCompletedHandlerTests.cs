// <copyright file="WorkflowStepCompletedHandlerTests.cs" company="Synaxis">
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
using Xunit;

/// <summary>
/// Unit tests for <see cref="WorkflowStepCompletedHandler"/>.
/// </summary>
public class WorkflowStepCompletedHandlerTests
{
    private readonly Mock<ILogger<WorkflowStepCompletedHandler>> _loggerMock;
    private readonly WorkflowStepCompletedHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowStepCompletedHandlerTests"/> class.
    /// </summary>
    public WorkflowStepCompletedHandlerTests()
    {
        this._loggerMock = new Mock<ILogger<WorkflowStepCompletedHandler>>();
        this._handler = new WorkflowStepCompletedHandler(this._loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidEvent_LogsInformationWithWorkflowStepDetails()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var notification = new WorkflowStepCompleted
        {
            Id = workflowId,
            StepNumber = 3,
            StepName = "ProcessData",
            CompletedAt = DateTime.UtcNow,
            Timestamp = DateTimeOffset.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Workflow step completed") && v.ToString().Contains(workflowId.ToString()) && v.ToString().Contains("3") && v.ToString().Contains("ProcessData")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}
