// <copyright file="AgentDeactivatedHandlerTests.cs" company="Synaxis">
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
/// Unit tests for <see cref="AgentDeactivatedHandler"/>.
/// </summary>
public class AgentDeactivatedHandlerTests
{
    private readonly Mock<ILogger<AgentDeactivatedHandler>> _loggerMock;
    private readonly AgentDeactivatedHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentDeactivatedHandlerTests"/> class.
    /// </summary>
    public AgentDeactivatedHandlerTests()
    {
        this._loggerMock = new Mock<ILogger<AgentDeactivatedHandler>>();
        this._handler = new AgentDeactivatedHandler(this._loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidEvent_LogsInformationWithAgentId()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var notification = new AgentDeactivated
        {
            AgentId = agentId,
            Version = 1,
            Timestamp = DateTimeOffset.UtcNow,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Agent deactivated") && v.ToString().Contains(agentId.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}
