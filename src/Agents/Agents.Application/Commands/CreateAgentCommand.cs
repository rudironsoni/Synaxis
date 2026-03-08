// <copyright file="CreateAgentCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.Commands;

using Synaxis.Shared.Contracts;

using Mediator;
using Synaxis.Agents.Application.DTOs;

/// <summary>
/// Command to create a new agent configuration.
/// </summary>
public sealed record CreateAgentCommand : Synaxis.Shared.Contracts.V2.Commands.CreateAgentCommand, IRequest<AgentDto>;
