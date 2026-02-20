// <copyright file="AgentConfigurationServiceTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.Tests.Services;

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Synaxis.Agents.Application.DTOs;
using Synaxis.Agents.Application.Interfaces;
using Synaxis.Agents.Application.Services;
using Synaxis.Agents.Domain.Aggregates;
using Synaxis.Agents.Domain.ValueObjects;
using Xunit;

/// <summary>
/// Unit tests for <see cref="AgentConfigurationService"/>.
/// </summary>
public class AgentConfigurationServiceTests
{
    private readonly Mock<IAgentConfigurationRepository> _repositoryMock;
    private readonly Mock<ILogger<AgentConfigurationService>> _loggerMock;
    private readonly AgentConfigurationService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentConfigurationServiceTests"/> class.
    /// </summary>
    public AgentConfigurationServiceTests()
    {
        this._repositoryMock = new Mock<IAgentConfigurationRepository>();
        this._loggerMock = new Mock<ILogger<AgentConfigurationService>>();
        this._service = new AgentConfigurationService(
            this._repositoryMock.Object,
            this._loggerMock.Object);
    }

    [Fact]
    public async Task CreateAgentAsync_WithValidRequest_ReturnsAgentDto()
    {
        // Arrange
        var request = new CreateAgentRequest
        {
            Name = "Test Agent",
            Description = "Test Description",
            AgentType = "Declarative",
            ConfigurationYaml = "config: test",
            TenantId = Guid.NewGuid(),
        };

        AgentConfiguration savedAgent = null;
        this._repositoryMock
            .Setup(r => r.SaveAsync(It.IsAny<AgentConfiguration>(), It.IsAny<CancellationToken>()))
            .Callback<AgentConfiguration, CancellationToken>((agent, _) => savedAgent = agent)
            .Returns(Task.CompletedTask);

        // Act
        var result = await this._service.CreateAgentAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Test Agent");
        result.Description.Should().Be("Test Description");
        result.AgentType.Should().Be("Declarative");
        result.Status.Should().Be(AgentStatus.Active);

        this._repositoryMock.Verify(r => r.SaveAsync(It.IsAny<AgentConfiguration>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAgentAsync_WithExistingAgent_ReturnsAgentDto()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = AgentConfiguration.Create(
            agentId,
            "Test Agent",
            "Description",
            "Declarative",
            "config: test",
            Guid.NewGuid(),
            null,
            null);

        this._repositoryMock
            .Setup(r => r.GetByIdAsync(agentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);

        // Act
        var result = await this._service.GetAgentAsync(agentId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(agentId);
        result.Name.Should().Be("Test Agent");
    }

    [Fact]
    public async Task GetAgentAsync_WithNonExistingAgent_ReturnsNull()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        this._repositoryMock
            .Setup(r => r.GetByIdAsync(agentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AgentConfiguration)null);

        // Act
        var result = await this._service.GetAgentAsync(agentId, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAgentAsync_WithExistingAgent_DeletesAgent()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var agent = AgentConfiguration.Create(
            agentId,
            "Test Agent",
            "Description",
            "Declarative",
            "config: test",
            Guid.NewGuid(),
            null,
            null);

        this._repositoryMock
            .Setup(r => r.GetByIdAsync(agentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);

        this._repositoryMock
            .Setup(r => r.DeleteAsync(agentId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await this._service.DeleteAgentAsync(agentId, CancellationToken.None);

        // Assert
        this._repositoryMock.Verify(r => r.DeleteAsync(agentId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
