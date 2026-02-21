// <copyright file="AgentConfigurationTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.UnitTests.Aggregates;

using System;
using FluentAssertions;
using Synaxis.Agents.Domain.Aggregates;
using Synaxis.Agents.Domain.ValueObjects;
using Xunit;

[Trait("Category", "Unit")]
public class AgentConfigurationTests
{
    [Fact]
    public void Constructor_ValidData_CreatesAgent()
    {
        // Arrange
        var id = Guid.NewGuid();
        var name = "Test Agent";
        var description = "A test agent";
        var agentType = "declarative";
        var configurationYaml = "name: Test Agent\ntype: declarative\nsteps: []";
        var tenantId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act
        var agent = AgentConfiguration.Create(
            id,
            name,
            description,
            agentType,
            configurationYaml,
            tenantId,
            teamId,
            userId);

        // Assert
        agent.Should().NotBeNull();
        agent.Id.Should().Be(id);
        agent.Name.Should().Be(name);
        agent.Description.Should().Be(description);
        agent.AgentType.Should().Be(agentType);
        agent.ConfigurationYaml.Should().Be(configurationYaml);
        agent.TenantId.Should().Be(tenantId);
        agent.TeamId.Should().Be(teamId);
        agent.UserId.Should().Be(userId);
        agent.Status.Should().Be(AgentStatus.Active);
        agent.Version.Should().Be(1);
        agent.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        agent.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdateConfiguration_UpdatesPropertiesAndRaisesEvent()
    {
        // Arrange
        var agent = AgentConfiguration.Create(
            Guid.NewGuid(),
            "Original Name",
            "Original Description",
            "declarative",
            "name: Original\nsteps: []",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());

        var newName = "Updated Name";
        var newDescription = "Updated Description";
        var newYaml = "name: Updated\nsteps: []";
        var originalVersion = agent.Version;

        // Act
        agent.Update(newName, newDescription, newYaml);

        // Assert
        agent.Name.Should().Be(newName);
        agent.Description.Should().Be(newDescription);
        agent.ConfigurationYaml.Should().Be(newYaml);
        agent.Version.Should().Be(originalVersion + 1);
        agent.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Activate_ChangesStatusToActive()
    {
        // Arrange
        var agent = AgentConfiguration.Create(
            Guid.NewGuid(),
            "Test Agent",
            null,
            "declarative",
            "name: Test\nsteps: []",
            Guid.NewGuid(),
            null,
            null);

        agent.Deactivate();
        var originalVersion = agent.Version;

        // Act
        agent.Activate();

        // Assert
        agent.Status.Should().Be(AgentStatus.Active);
        agent.Version.Should().Be(originalVersion + 1);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_DoesNotChangeStatus()
    {
        // Arrange
        var agent = AgentConfiguration.Create(
            Guid.NewGuid(),
            "Test Agent",
            null,
            "declarative",
            "name: Test\nsteps: []",
            Guid.NewGuid(),
            null,
            null);

        var originalVersion = agent.Version;

        // Act
        agent.Activate();

        // Assert
        agent.Status.Should().Be(AgentStatus.Active);
        agent.Version.Should().Be(originalVersion);
    }

    [Fact]
    public void Deactivate_ChangesStatusToInactive()
    {
        // Arrange
        var agent = AgentConfiguration.Create(
            Guid.NewGuid(),
            "Test Agent",
            null,
            "declarative",
            "name: Test\nsteps: []",
            Guid.NewGuid(),
            null,
            null);

        var originalVersion = agent.Version;

        // Act
        agent.Deactivate();

        // Assert
        agent.Status.Should().Be(AgentStatus.Inactive);
        agent.Version.Should().Be(originalVersion + 1);
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_DoesNotChangeStatus()
    {
        // Arrange
        var agent = AgentConfiguration.Create(
            Guid.NewGuid(),
            "Test Agent",
            null,
            "declarative",
            "name: Test\nsteps: []",
            Guid.NewGuid(),
            null,
            null);

        agent.Deactivate();
        var originalVersion = agent.Version;

        // Act
        agent.Deactivate();

        // Assert
        agent.Status.Should().Be(AgentStatus.Inactive);
        agent.Version.Should().Be(originalVersion);
    }
}
