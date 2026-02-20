// <copyright file="DeleteAgentCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Agents.Application.Commands;

using Mediator;

/// <summary>
/// Command to delete an agent configuration.
/// </summary>
public sealed record DeleteAgentCommand : Contracts.V2.Commands.DeleteAgentCommand, IRequest<Unit>;
