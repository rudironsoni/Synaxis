// <copyright file="WorkflowRetriedHandlerTests.cs" company="Synaxis">
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
/// Unit tests for <see cref="WorkflowRetriedHandler"/>.
/// </summary>
public class WorkflowRetriedHandlerTests
{
    private readonly Mock<ILogger<WorkflowRetriedHandler>> _loggerMock;
    private readonly WorkflowRetriedHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowRetriedHandlerTests"/> class.
    /// </summary>
    public WorkflowRetriedHandlerTests()
    {
        this._loggerMock = new Mock<ILogger<WorkflowRetriedHandler>>();
        this._handler = new WorkflowRetriedHandler(this._loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidEvent_LogsWarningWithWorkflowRetryDetails()
    {
        // Arrange
        var workflowId = Guid.NewGuid();
        var notification = new WorkflowRetried
        {
            Id = workflowId,
            StepNumber = 3,
            RetryAttempt = 2,
            RetriedAt = DateTime.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Workflow retried") && v.ToString().Contains(workflowId.ToString()) && v.ToString().Contains("3") && v.ToString().Contains("2")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}
