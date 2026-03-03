// <copyright file="CompleteInferenceCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Application.Commands;

using MediatR;
using Microsoft.Extensions.AI;

/// <summary>
/// Command to complete an inference request.
/// </summary>
/// <param name="RequestId">The request identifier.</param>
/// <param name="ResponseContent">The response content.</param>
/// <param name="TokenUsage">The token usage.</param>
/// <param name="Cost">The cost.</param>
/// <param name="LatencyMs">The latency in milliseconds.</param>
public record CompleteInferenceCommand(
    Guid RequestId,
    string ResponseContent,
    TokenUsage TokenUsage,
    decimal Cost,
    long LatencyMs)
    : IRequest<CompleteInferenceResult>;
