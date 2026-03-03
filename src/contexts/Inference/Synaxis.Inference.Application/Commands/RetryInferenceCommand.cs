// <copyright file="RetryInferenceCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Application.Commands;

using MediatR;

/// <summary>
/// Command to retry a failed inference request.
/// </summary>
/// <param name="RequestId">The request identifier.</param>
/// <param name="NewProviderId">The new provider to use.</param>
public record RetryInferenceCommand(Guid RequestId, string NewProviderId)
    : IRequest<RetryInferenceResult>;
