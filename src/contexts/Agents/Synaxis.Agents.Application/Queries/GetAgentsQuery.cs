// <copyright file="GetAgentsQuery.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.Queries;

using Mediator;
using Synaxis.Contracts.V2.DTOs;

/// <summary>
/// Query to get a paginated list of agents.
/// </summary>
public sealed record GetAgentsQuery : Contracts.V2.Queries.GetAgentsQuery, IRequest<PaginatedResult<AgentDto>>;
