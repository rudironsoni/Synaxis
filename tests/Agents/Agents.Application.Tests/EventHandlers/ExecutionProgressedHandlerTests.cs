// <copyright file="ExecutionProgressedHandlerTests.cs" company="Synaxis">
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
using Synaxis.Agents.Domain.ValueObjects;
using Xunit;

/// <summary>
/// Unit tests for <see cref="ExecutionProgressedHandler"/>.
/// </summary>
public class ExecutionProgressedHandlerTests
{
    private readonly Mock<ILogger<ExecutionProgressedHandler>> _loggerMock;
    private readonly ExecutionProgressedHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutionProgressedHandlerTests"/> class.
    /// </summary>
    public ExecutionProgressedHandlerTests()
    {
        this._loggerMock = new Mock<ILogger<ExecutionProgressedHandler>>();
        this._handler = new ExecutionProgressedHandler(this._loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidEvent_LogsDebugWithExecutionIdAndStep()
    {
        // Arrange
        var executionId = Guid.NewGuid();
        var notification = new ExecutionProgressed
        {
            Id = executionId,
            CurrentStep = 5,
            Step = new ExecutionStep
            {
                StepNumber = 5,
                Name = "TestStep",
                Status = AgentStatus.Active,
            },
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Execution progressed") && v.ToString().Contains(executionId.ToString()) && v.ToString().Contains("5")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}
