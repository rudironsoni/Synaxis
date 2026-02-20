// <copyright file="GetAgentWorkflowsQuery.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.Queries;

using Mediator;
using Synaxis.Contracts.V2.DTOs;
using Synaxis.Contracts.V2.Queries;

/// <summary>
/// Query to get workflows with optional filtering and pagination.
/// </summary>
public record GetAgentWorkflowsQuery : QueryBase, IRequest<PaginatedResult<WorkflowDto>>
{
    /// <summary>
    /// Gets filter by workflow status.
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
    public string SortBy { get; init; } = "createdAt";

    /// <summary>
    /// Gets sort direction (asc or desc).
    /// </summary>
    public string SortDirection { get; init; } = "desc";

    /// <summary>
    /// Gets filter by tenant identifier (overrides base TenantId).
    /// </summary>
    public new string? TenantId { get; init; }
}
