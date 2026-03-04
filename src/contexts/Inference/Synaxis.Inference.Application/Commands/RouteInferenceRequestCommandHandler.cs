// <copyright file="RouteInferenceRequestCommandHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Application.Commands;

using MediatR;
using Microsoft.Extensions.Logging;
using Synaxis.Inference.Domain.ValueObjects;

/// <summary>
/// Handler for <see cref="RouteInferenceRequestCommand"/>.
/// </summary>
public class RouteInferenceRequestCommandHandler : IRequestHandler<RouteInferenceRequestCommand, RouteInferenceRequestResult>
{
    private readonly ILogger<RouteInferenceRequestCommandHandler> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RouteInferenceRequestCommandHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public RouteInferenceRequestCommandHandler(ILogger<RouteInferenceRequestCommandHandler> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc/>
    public Task<RouteInferenceRequestResult> Handle(RouteInferenceRequestCommand request, CancellationToken cancellationToken)
    {
        this.logger.LogInformation(
            "Routing inference request {RequestId} with preferred provider {PreferredProvider}",
            request.RequestId,
            request.PreferredProvider ?? "(none)");

        // Placeholder implementation - in production this would use the routing service
        var decision = new RoutingDecision
        {
            ProviderId = request.PreferredProvider ?? "openai",
            ModelId = "gpt-4",
            Reason = "Default routing",
            Score = 1.0,
            EstimatedLatencyMs = 500,
            EstimatedCost = 0.01m,
        };

        var result = new RouteInferenceRequestResult(request.RequestId, decision);
        return Task.FromResult(result);
    }
}
