// <copyright file="RouteInferenceRequestCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Application.Commands;

using MediatR;

/// <summary>
/// Command to route an inference request to a provider.
/// </summary>
/// <param name="RequestId">The request identifier.</param>
/// <param name="PreferredProvider">The optional preferred provider.</param>
/// <param name="EnableStreaming">Whether streaming is enabled.</param>
public record RouteInferenceRequestCommand(
    Guid RequestId,
    string? PreferredProvider = null,
    bool EnableStreaming = false)
    : IRequest<RouteInferenceRequestResult>;
