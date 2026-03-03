// <copyright file="ExecuteAgentCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.Commands;

using Mediator;

/// <summary>
/// Command to execute an agent.
/// </summary>
public sealed record ExecuteAgentCommand : Contracts.V2.Commands.ExecuteAgentCommand, IRequest<Guid>;
