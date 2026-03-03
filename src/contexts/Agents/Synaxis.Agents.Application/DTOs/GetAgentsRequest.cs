// <copyright file="GetAgentsRequest.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.DTOs;

using Synaxis.Agents.Domain.ValueObjects;

/// <summary>
/// Request to get agents with pagination and filtering.
/// </summary>
public record GetAgentsRequest
{
    /// <summary>
    /// Gets page number (1-based).
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Gets number of items per page.
    /// </summary>
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// Gets tenant identifier to filter by.
    /// </summary>
    public Guid? TenantId { get; init; }

    /// <summary>
    /// Gets status to filter by.
    /// </summary>
    public AgentStatus? Status { get; init; }

    /// <summary>
    /// Gets search term to filter by name.
    /// </summary>
    public string? SearchTerm { get; init; }
}
