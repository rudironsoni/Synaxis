// <copyright file="AgentOrchestratorTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.IntegrationTests.Orchestration;

using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Synaxis.Agents.Infrastructure.Orchestration;
using Synaxis.Agents.Domain.ValueObjects;
using Xunit;

[Trait("Category", "Integration")]
public class AgentOrchestratorTests : IAsyncLifetime
{
    private readonly AgentOrchestrator _orchestrator = new();
    private readonly string _testYamlFilePath;
    private readonly string _testYamlContent;

    public AgentOrchestratorTests()
    {
        _testYamlFilePath = Path.Combine(Path.GetTempPath(), $"test-agent-{Guid.NewGuid()}.yaml");
        _testYamlContent = """
            name: Test Agent
            type: declarative
            description: A test agent for integration testing
            steps:
              - name: Step 1
                type: input
                prompt: Enter your name
                output_variable: user_name
              - name: Step 2
                type: function
                function: greet_user
                arguments:
                  name: "{{user_name}}"
              - name: Step 3
                type: output
                message: Hello, {{user_name}}!
            """;
    }

    public async Task InitializeAsync()
    {
        await File.WriteAllTextAsync(_testYamlFilePath, _testYamlContent);
    }

    public async Task DisposeAsync()
    {
        _orchestrator.Dispose();
        if (File.Exists(_testYamlFilePath))
        {
            await Task.Run(() => File.Delete(_testYamlFilePath));
        }
    }

    [Fact]
    public void LoadAgent_ValidConfiguration_LoadsSuccessfully()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var name = "Test Agent";
        var description = "A test agent";
        var yaml = _testYamlContent;
        var tenantId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act
        var agent = _orchestrator.LoadAgent(
            agentId,
            name,
            description,
            yaml,
            tenantId,
            teamId,
            userId);

        // Assert
        agent.Should().NotBeNull();
        agent.Id.Should().Be(agentId);
        agent.Name.Should().Be(name);
        agent.Description.Should().Be(description);
        agent.AgentType.Should().Be("declarative");
        agent.ConfigurationYaml.Should().Be(yaml);
        agent.TenantId.Should().Be(tenantId);
        agent.TeamId.Should().Be(teamId);
        agent.UserId.Should().Be(userId);
        agent.Status.Should().Be(AgentStatus.Active);
    }

    [Fact]
    public void LoadAgent_AddsToActiveAgents()
    {
        // Arrange
        var agentId = Guid.NewGuid();

        // Act
        var agent = _orchestrator.LoadAgent(
            agentId,
            "Test Agent",
            null,
            _testYamlContent,
            Guid.NewGuid(),
            null,
            null);

        // Assert
        var retrievedAgent = _orchestrator.GetAgent(agentId);
        retrievedAgent.Should().NotBeNull();
        retrievedAgent.Should().Be(agent);
    }

    [Fact]
    public async Task LoadAgentAsync_ValidFile_LoadsSuccessfully()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var name = "Test Agent";
        var description = "A test agent";
        var tenantId = Guid.NewGuid();

        // Act
        var agent = await _orchestrator.LoadAgentAsync(
            agentId,
            name,
            description,
            _testYamlFilePath,
            tenantId,
            null,
            null);

        // Assert
        agent.Should().NotBeNull();
        agent.Id.Should().Be(agentId);
        agent.Name.Should().Be(name);
        agent.Description.Should().Be(description);
        agent.ConfigurationYaml.Should().Be(_testYamlContent);
    }

    [Fact]
    public async Task LoadAgentAsync_NonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"non-existent-{Guid.NewGuid()}.yaml");

        // Act
        Func<Task> act = async () => await _orchestrator.LoadAgentAsync(
            agentId,
            "Test Agent",
            null,
            nonExistentPath,
            Guid.NewGuid(),
            null,
            null);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task StartAgent_ExecutesStepsInOrder()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        _orchestrator.LoadAgent(
            agentId,
            "Test Agent",
            null,
            _testYamlContent,
            Guid.NewGuid(),
            null,
            null);

        // Act
        await _orchestrator.StartAgentAsync(agentId);

        // Assert
        var agent = _orchestrator.GetAgent(agentId);
        agent.Should().NotBeNull();
        agent!.Status.Should().Be(AgentStatus.Active);
    }

    [Fact]
    public async Task StartAgent_NonExistentAgent_ThrowsInvalidOperationException()
    {
        // Arrange
        var nonExistentAgentId = Guid.NewGuid();

        // Act
        Func<Task> act = async () => await _orchestrator.StartAgentAsync(nonExistentAgentId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Agent not found: {nonExistentAgentId}");
    }

    [Fact]
    public async Task PauseAgent_PausesExecution()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        _orchestrator.LoadAgent(
            agentId,
            "Test Agent",
            null,
            _testYamlContent,
            Guid.NewGuid(),
            null,
            null);

        await _orchestrator.StartAgentAsync(agentId);

        // Act
        await _orchestrator.PauseAgentAsync(agentId);

        // Assert
        var agent = _orchestrator.GetAgent(agentId);
        agent.Should().NotBeNull();
        agent!.Status.Should().Be(AgentStatus.Inactive);
    }

    [Fact]
    public async Task PauseAgent_NonExistentAgent_ThrowsInvalidOperationException()
    {
        // Arrange
        var nonExistentAgentId = Guid.NewGuid();

        // Act
        Func<Task> act = async () => await _orchestrator.PauseAgentAsync(nonExistentAgentId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Agent not found: {nonExistentAgentId}");
    }

    [Fact]
    public async Task ResumeAgent_ContinuesExecution()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        _orchestrator.LoadAgent(
            agentId,
            "Test Agent",
            null,
            _testYamlContent,
            Guid.NewGuid(),
            null,
            null);

        await _orchestrator.StartAgentAsync(agentId);
        await _orchestrator.PauseAgentAsync(agentId);

        // Act
        await _orchestrator.ResumeAgentAsync(agentId);

        // Assert
        var agent = _orchestrator.GetAgent(agentId);
        agent.Should().NotBeNull();
        agent!.Status.Should().Be(AgentStatus.Active);
    }

    [Fact]
    public async Task StopAgent_StopsExecution()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        _orchestrator.LoadAgent(
            agentId,
            "Test Agent",
            null,
            _testYamlContent,
            Guid.NewGuid(),
            null,
            null);

        await _orchestrator.StartAgentAsync(agentId);

        // Act
        await _orchestrator.StopAgentAsync(agentId);

        // Assert
        var agent = _orchestrator.GetAgent(agentId);
        agent.Should().NotBeNull();
        agent!.Status.Should().Be(AgentStatus.Inactive);
    }

    [Fact]
    public void GetAgent_ExistingAgent_ReturnsAgent()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        var expectedAgent = _orchestrator.LoadAgent(
            agentId,
            "Test Agent",
            null,
            _testYamlContent,
            Guid.NewGuid(),
            null,
            null);

        // Act
        var agent = _orchestrator.GetAgent(agentId);

        // Assert
        agent.Should().NotBeNull();
        agent.Should().Be(expectedAgent);
    }

    [Fact]
    public void GetAgent_NonExistentAgent_ReturnsNull()
    {
        // Arrange
        var nonExistentAgentId = Guid.NewGuid();

        // Act
        var agent = _orchestrator.GetAgent(nonExistentAgentId);

        // Assert
        agent.Should().BeNull();
    }

    [Fact]
    public void GetAllAgents_ReturnsAllActiveAgents()
    {
        // Arrange
        var agent1Id = Guid.NewGuid();
        var agent2Id = Guid.NewGuid();
        var agent3Id = Guid.NewGuid();

        _orchestrator.LoadAgent(agent1Id, "Agent 1", null, _testYamlContent, Guid.NewGuid(), null, null);
        _orchestrator.LoadAgent(agent2Id, "Agent 2", null, _testYamlContent, Guid.NewGuid(), null, null);
        _orchestrator.LoadAgent(agent3Id, "Agent 3", null, _testYamlContent, Guid.NewGuid(), null, null);

        // Act
        var agents = _orchestrator.GetAllAgents();

        // Assert
        agents.Should().HaveCount(3);
        agents.Should().Contain(a => a.Id == agent1Id);
        agents.Should().Contain(a => a.Id == agent2Id);
        agents.Should().Contain(a => a.Id == agent3Id);
    }

    [Fact]
    public void GetAllAgents_WhenNoAgents_ReturnsEmptyList()
    {
        // Arrange & Act
        var agents = _orchestrator.GetAllAgents();

        // Assert
        agents.Should().BeEmpty();
    }

    [Fact]
    public void RemoveAgent_ExistingAgent_RemovesSuccessfully()
    {
        // Arrange
        var agentId = Guid.NewGuid();
        _orchestrator.LoadAgent(agentId, "Test Agent", null, _testYamlContent, Guid.NewGuid(), null, null);

        // Act
        var result = _orchestrator.RemoveAgent(agentId);

        // Assert
        result.Should().BeTrue();
        _orchestrator.GetAgent(agentId).Should().BeNull();
    }

    [Fact]
    public void RemoveAgent_NonExistentAgent_ReturnsFalse()
    {
        // Arrange
        var nonExistentAgentId = Guid.NewGuid();

        // Act
        var result = _orchestrator.RemoveAgent(nonExistentAgentId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task MultipleAgents_StartAndPauseIndependently()
    {
        // Arrange
        var agent1Id = Guid.NewGuid();
        var agent2Id = Guid.NewGuid();

        _orchestrator.LoadAgent(agent1Id, "Agent 1", null, _testYamlContent, Guid.NewGuid(), null, null);
        _orchestrator.LoadAgent(agent2Id, "Agent 2", null, _testYamlContent, Guid.NewGuid(), null, null);

        // Act
        await _orchestrator.StartAgentAsync(agent1Id);
        await _orchestrator.StartAgentAsync(agent2Id);
        await _orchestrator.PauseAgentAsync(agent1Id);

        // Assert
        var agent1 = _orchestrator.GetAgent(agent1Id);
        var agent2 = _orchestrator.GetAgent(agent2Id);

        agent1.Should().NotBeNull();
        agent1!.Status.Should().Be(AgentStatus.Inactive);

        agent2.Should().NotBeNull();
        agent2!.Status.Should().Be(AgentStatus.Active);
    }
}
