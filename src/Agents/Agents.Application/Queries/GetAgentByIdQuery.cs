// <copyright file="GetAgentByIdQuery.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.Queries;

using Mediator;
using Synaxis.Contracts.V2.DTOs;

/// <summary>
/// Query to get an agent by its identifier.
/// </summary>
public sealed record GetAgentByIdQuery : Contracts.V2.Queries.GetAgentByIdQuery, IRequest<AgentDto?>;
