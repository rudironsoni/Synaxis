// <copyright file="UpdateAgentCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.Commands;

using Mediator;
using Synaxis.Agents.Application.DTOs;

/// <summary>
/// Command to update an existing agent configuration.
/// </summary>
public sealed record UpdateAgentCommand : Contracts.V2.Commands.UpdateAgentCommand, IRequest<AgentDto>;
