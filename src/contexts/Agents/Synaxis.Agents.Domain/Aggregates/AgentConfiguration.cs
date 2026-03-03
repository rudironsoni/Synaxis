// <copyright file="AgentConfiguration.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Domain.Aggregates;

using System;
using System.Collections.Generic;
using Synaxis.Abstractions.Cloud;
using Synaxis.Agents.Domain.Events;
using Synaxis.Agents.Domain.ValueObjects;
using Synaxis.Infrastructure.EventSourcing;

/// <summary>
/// Aggregate root representing an agent configuration.
/// </summary>
public class AgentConfiguration : AggregateRoot
{
    /// <summary>
    /// Gets the agent identifier.
    /// </summary>
    public new Guid Id { get; private set; }

    /// <summary>
    /// Gets the agent name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the agent description.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets the agent type.
    /// </summary>
    public string AgentType { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the configuration YAML.
    /// </summary>
    public string ConfigurationYaml { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the version number.
    /// </summary>
    public new int Version { get; private set; }

    /// <summary>
    /// Gets the status.
    /// </summary>
    public AgentStatus Status { get; private set; }

    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Gets the team identifier.
    /// </summary>
    public Guid? TeamId { get; private set; }

    /// <summary>
    /// Gets the user identifier.
    /// </summary>
    public Guid? UserId { get; private set; }

    /// <summary>
    /// Gets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Creates a new agent configuration.
    /// </summary>
    /// <param name="id">The agent identifier.</param>
    /// <param name="name">The agent name.</param>
    /// <param name="description">The agent description.</param>
    /// <param name="agentType">The agent type.</param>
    /// <param name="configurationYaml">The configuration YAML.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="teamId">The team identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <returns>A new agent configuration instance.</returns>
    public static AgentConfiguration Create(
        Guid id,
        string name,
        string? description,
        string agentType,
        string configurationYaml,
        Guid tenantId,
        Guid? teamId,
        Guid? userId)
    {
        var agent = new AgentConfiguration();
        var @event = new AgentConfigurationCreated
        {
            AgentId = id,
            Name = name,
            Description = description,
            AgentType = agentType,
            ConfigurationYaml = configurationYaml,
            TenantId = tenantId,
            TeamId = teamId,
            UserId = userId,
            Timestamp = DateTime.UtcNow,
            Version = 1,
        };

        agent.ApplyEvent(@event);
        return agent;
    }

    /// <summary>
    /// Updates the agent configuration.
    /// </summary>
    /// <param name="name">The agent name.</param>
    /// <param name="description">The agent description.</param>
    /// <param name="configurationYaml">The configuration YAML.</param>
    public void Update(string name, string? description, string configurationYaml)
    {
        var @event = new AgentConfigurationUpdated
        {
            AgentId = this.Id,
            Name = name,
            Description = description,
            ConfigurationYaml = configurationYaml,
            Timestamp = DateTime.UtcNow,
            Version = this.Version + 1,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Activates the agent.
    /// </summary>
    public void Activate()
    {
        if (this.Status == AgentStatus.Active)
        {
            return;
        }

        var @event = new AgentActivated
        {
            AgentId = this.Id,
            Timestamp = DateTime.UtcNow,
            Version = this.Version + 1,
        };

        this.ApplyEvent(@event);
    }

    /// <summary>
    /// Deactivates the agent.
    /// </summary>
    public void Deactivate()
    {
        if (this.Status == AgentStatus.Inactive)
        {
            return;
        }

        var @event = new AgentDeactivated
        {
            AgentId = this.Id,
            Timestamp = DateTime.UtcNow,
            Version = this.Version + 1,
        };

        this.ApplyEvent(@event);
    }

    /// <inheritdoc/>
    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case AgentConfigurationCreated created:
                this.ApplyCreated(created);
                break;
            case AgentConfigurationUpdated updated:
                this.ApplyUpdated(updated);
                break;
            case AgentActivated activated:
                this.ApplyActivated(activated);
                break;
            case AgentDeactivated deactivated:
                this.ApplyDeactivated(deactivated);
                break;
        }
    }

    private void ApplyCreated(AgentConfigurationCreated @event)
    {
        this.Id = @event.AgentId;
        this.Name = @event.Name;
        this.Description = @event.Description;
        this.AgentType = @event.AgentType;
        this.ConfigurationYaml = @event.ConfigurationYaml;
        this.TenantId = @event.TenantId;
        this.TeamId = @event.TeamId;
        this.UserId = @event.UserId;
        this.Version = @event.Version;
        this.Status = AgentStatus.Active;
        this.CreatedAt = @event.Timestamp;
        this.UpdatedAt = @event.Timestamp;
    }

    private void ApplyUpdated(AgentConfigurationUpdated @event)
    {
        this.Name = @event.Name;
        this.Description = @event.Description;
        this.ConfigurationYaml = @event.ConfigurationYaml;
        this.Version = @event.Version;
        this.UpdatedAt = @event.Timestamp.UtcDateTime;
    }

    private void ApplyActivated(AgentActivated @event)
    {
        this.Status = AgentStatus.Active;
        this.Version = @event.Version;
        this.UpdatedAt = @event.Timestamp.UtcDateTime;
    }

    private void ApplyDeactivated(AgentDeactivated @event)
    {
        this.Status = AgentStatus.Inactive;
        this.Version = @event.Version;
        this.UpdatedAt = @event.Timestamp.UtcDateTime;
    }
}
