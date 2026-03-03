// <copyright file="RouteInferenceRequestResult.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Application.Commands;

/// <summary>
/// Result of routing an inference request.
/// </summary>
/// <param name="RequestId">The request identifier.</param>
/// <param name="ProviderId">The selected provider identifier.</param>
/// <param name="ModelId">The resolved model identifier.</param>
/// <param name="RoutingDecision">The routing decision details.</param>
public record RouteInferenceRequestResult(
    Guid RequestId,
    string ProviderId,
    string ModelId,
    RoutingDecision RoutingDecision);
