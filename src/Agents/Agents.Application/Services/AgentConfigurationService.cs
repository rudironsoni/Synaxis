// <copyright file="AgentConfigurationService.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.Services;

using Microsoft.Extensions.Logging;
using Synaxis.Agents.Application.DTOs;
using Synaxis.Agents.Application.Interfaces;
using Synaxis.Agents.Domain.Aggregates;
using Synaxis.Agents.Domain.ValueObjects;

/// <summary>
/// Implementation of agent configuration service.
/// </summary>
public class AgentConfigurationService : IAgentConfigurationService
{
    private readonly IAgentConfigurationRepository _repository;
    private readonly ILogger<AgentConfigurationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentConfigurationService"/> class.
    /// </summary>
    /// <param name="repository">The agent configuration repository.</param>
    /// <param name="logger">The logger.</param>
    public AgentConfigurationService(
        IAgentConfigurationRepository repository,
        ILogger<AgentConfigurationService> logger)
    {
        this._repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<AgentDto> CreateAgentAsync(
        CreateAgentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var id = Guid.NewGuid();
        var agent = AgentConfiguration.Create(
            id,
            request.Name,
            request.Description,
            request.AgentType,
            request.ConfigurationYaml,
            request.TenantId,
            request.TeamId,
            request.UserId);

        await this._repository.SaveAsync(agent, cancellationToken).ConfigureAwait(false);

        this._logger.LogInformation(
            "Created agent configuration {AgentId} with name {AgentName} for tenant {TenantId}",
            id,
            request.Name,
            request.TenantId);

        return this.MapToDto(agent);
    }

    /// <inheritdoc/>
    public async Task<AgentDto> UpdateAgentAsync(
        Guid id,
        UpdateAgentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var agent = await this._repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (agent == null)
        {
            throw new InvalidOperationException($"Agent configuration with ID {id} not found.");
        }

        agent.Update(request.Name, request.Description, request.ConfigurationYaml);

        await this._repository.SaveAsync(agent, cancellationToken).ConfigureAwait(false);

        this._logger.LogInformation(
            "Updated agent configuration {AgentId} with name {AgentName}",
            id,
            request.Name);

        return this.MapToDto(agent);
    }

    /// <inheritdoc/>
    public async Task DeleteAgentAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await this._repository.DeleteAsync(id, cancellationToken).ConfigureAwait(false);

        this._logger.LogInformation("Deleted agent configuration {AgentId}", id);
    }

    /// <inheritdoc/>
    public async Task<AgentDto?> GetAgentAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var agent = await this._repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return agent == null ? null : this.MapToDto(agent);
    }

    /// <inheritdoc/>
    public async Task<PaginatedResult<AgentDto>> GetAgentsAsync(
        GetAgentsRequest request,
        CancellationToken cancellationToken = default)
    {
        var agents = await this._repository.GetByTenantAsync(
            request.TenantId ?? Guid.Empty,
            cancellationToken).ConfigureAwait(false);

        // Apply status filter if specified
        if (request.Status.HasValue)
        {
            agents = agents.Where(a => a.Status == request.Status.Value).ToList();
        }

        // Apply search filter if specified
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLowerInvariant();
            agents = agents.Where(a =>
                a.Name.ToLowerInvariant().Contains(searchTerm) ||
                (a.Description?.ToLowerInvariant().Contains(searchTerm) ?? false)).ToList();
        }

        var totalCount = agents.Count;
        var pageSize = Math.Max(1, request.PageSize);
        var page = Math.Max(1, request.Page);

        var pagedAgents = agents
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(this.MapToDto)
            .ToList();

        return new PaginatedResult<AgentDto>
        {
            Items = pagedAgents,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
        };
    }

    /// <inheritdoc/>
    public async Task<AgentDto> ActivateAgentAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var agent = await this._repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (agent == null)
        {
            throw new InvalidOperationException($"Agent configuration with ID {id} not found.");
        }

        agent.Activate();

        await this._repository.SaveAsync(agent, cancellationToken).ConfigureAwait(false);

        this._logger.LogInformation("Activated agent configuration {AgentId}", id);

        return this.MapToDto(agent);
    }

    /// <inheritdoc/>
    public async Task<AgentDto> DeactivateAgentAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var agent = await this._repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (agent == null)
        {
            throw new InvalidOperationException($"Agent configuration with ID {id} not found.");
        }

        agent.Deactivate();

        await this._repository.SaveAsync(agent, cancellationToken).ConfigureAwait(false);

        this._logger.LogInformation("Deactivated agent configuration {AgentId}", id);

        return this.MapToDto(agent);
    }

    private AgentDto MapToDto(AgentConfiguration agent)
    {
        return new AgentDto
        {
            Id = agent.Id,
            Name = agent.Name,
            Description = agent.Description,
            AgentType = agent.AgentType,
            Status = agent.Status,
            CreatedByUserId = agent.UserId?.ToString() ?? string.Empty,
            CreatedAt = new DateTimeOffset(agent.CreatedAt.Ticks, TimeSpan.Zero),
            UpdatedAt = agent.UpdatedAt == default ? null : new DateTimeOffset(agent.UpdatedAt.Ticks, TimeSpan.Zero),
        };
    }
}
