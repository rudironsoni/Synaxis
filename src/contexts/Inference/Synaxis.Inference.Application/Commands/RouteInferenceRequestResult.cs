// <copyright file="RouteInferenceRequestResult.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Application.Commands;

using Synaxis.Inference.Domain.ValueObjects;

/// <summary>
/// Result of routing an inference request.
/// </summary>
/// <param name="RequestId">The request identifier.</param>
/// <param name="Decision">The routing decision details.</param>
public record RouteInferenceRequestResult(
    Guid RequestId,
    RoutingDecision Decision);
