// <copyright file="ExecuteInferenceCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Application.Commands;

using MediatR;

/// <summary>
/// Command to execute an inference request.
/// </summary>
/// <param name="RequestId">The request identifier.</param>
public record ExecuteInferenceCommand(Guid RequestId)
    : IRequest<ExecuteInferenceResult>;
