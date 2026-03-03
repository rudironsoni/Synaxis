// <copyright file="CancelAgentExecutionCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.Commands;

using Mediator;

/// <summary>
/// Command to cancel an agent execution.
/// </summary>
public sealed record CancelAgentExecutionCommand : Contracts.V2.Commands.CancelAgentExecutionCommand, IRequest<Unit>;
