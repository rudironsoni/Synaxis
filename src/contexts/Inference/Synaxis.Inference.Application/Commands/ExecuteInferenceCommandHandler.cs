// <copyright file="ExecuteInferenceCommandHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Application.Commands;

using MediatR;
using Microsoft.Extensions.Logging;
using Synaxis.Inference.Domain.ValueObjects;

/// <summary>
/// Handler for <see cref="ExecuteInferenceCommand"/>.
/// </summary>
public class ExecuteInferenceCommandHandler : IRequestHandler<ExecuteInferenceCommand, ExecuteInferenceResult>
{
    private readonly ILogger<ExecuteInferenceCommandHandler> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecuteInferenceCommandHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ExecuteInferenceCommandHandler(ILogger<ExecuteInferenceCommandHandler> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc/>
    public Task<ExecuteInferenceResult> Handle(ExecuteInferenceCommand request, CancellationToken cancellationToken)
    {
        this.logger.LogInformation("Executing inference for request {RequestId}", request.RequestId);

        // Placeholder implementation - in production this would call the actual inference service
        var result = new ExecuteInferenceResult(
            request.RequestId,
            "This is a placeholder response from the Synaxis Inference API.",
            new TokenUsage(10, 20),
            100);

        return Task.FromResult(result);
    }
}
