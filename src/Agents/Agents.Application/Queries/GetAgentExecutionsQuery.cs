// <copyright file="GetAgentExecutionsQuery.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.Queries;

using Mediator;
using Synaxis.Contracts.V2.DTOs;
using Synaxis.Contracts.V2.Queries;

/// <summary>
/// Query to get executions for an agent with optional filtering and pagination.
/// </summary>
public record GetAgentExecutionsQuery : QueryBase, IRequest<PaginatedResult<ExecutionDto>>
{
    /// <summary>
    /// Gets identifier of the agent to get executions for.
    /// </summary>
    public required Guid AgentId { get; init; }

    /// <summary>
    /// Gets filter by execution status.
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// Gets page number (1-based).
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Gets number of items per page.
    /// </summary>
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// Gets sort field.
    /// </summary>
    public string SortBy { get; init; } = "startedAt";

    /// <summary>
    /// Gets sort direction (asc or desc).
    /// </summary>
    public string SortDirection { get; init; } = "desc";
}
