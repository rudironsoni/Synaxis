// <copyright file="AgentConfigurationDeletedHandlerTests.cs" company="Synaxis">
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
/// Unit tests for <see cref="AgentConfigurationDeletedHandler"/>.
/// </summary>
public class AgentConfigurationDeletedHandlerTests
{
    private readonly Mock<ILogger<AgentConfigurationDeletedHandler>> _loggerMock;
    private readonly AgentConfigurationDeletedHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentConfigurationDeletedHandlerTests"/> class.
    /// </summary>
    public AgentConfigurationDeletedHandlerTests()
    {
        this._loggerMock = new Mock<ILogger<AgentConfigurationDeletedHandler>>();
        this._handler = new AgentConfigurationDeletedHandler(this._loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidEvent_LogsWarningWithAgentId()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var notification = new AgentConfigurationDeleted
        {
            AgentId = agentId,
            Version = 1,
        };

        // Act
        await this._handler.Handle(notification, CancellationToken.None);

        // Assert
        this._loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Agent configuration deleted") && v.ToString().Contains(agentId.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}
