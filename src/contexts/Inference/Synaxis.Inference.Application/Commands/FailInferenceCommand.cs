// <copyright file="FailInferenceCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Application.Commands;

using MediatR;

/// <summary>
/// Command to fail an inference request.
/// </summary>
/// <param name="RequestId">The request identifier.</param>
/// <param name="ErrorMessage">The error message.</param>
public record FailInferenceCommand(Guid RequestId, string ErrorMessage)
    : IRequest<FailInferenceResult>;
