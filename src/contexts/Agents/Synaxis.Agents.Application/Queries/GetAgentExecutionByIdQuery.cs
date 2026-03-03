// <copyright file="GetAgentExecutionByIdQuery.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.Queries;

using Mediator;
using Synaxis.Contracts.V2.DTOs;
using Synaxis.Contracts.V2.Queries;

/// <summary>
/// Query to get a single execution by its identifier.
/// </summary>
public record GetAgentExecutionByIdQuery : QueryBase, IRequest<ExecutionDto?>
{
    /// <summary>
    /// Gets identifier of the execution to retrieve.
    /// </summary>
    public required Guid ExecutionId { get; init; }
}
