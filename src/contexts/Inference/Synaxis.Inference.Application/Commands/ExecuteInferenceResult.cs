// <copyright file="ExecuteInferenceResult.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Application.Commands;

using Microsoft.Extensions.AI;

/// <summary>
/// Result of executing an inference request.
/// </summary>
/// <param name="RequestId">The request identifier.</param>
/// <param name="ResponseContent">The response content.</param>
/// <param name="TokenUsage">The token usage.</param>
/// <param name="LatencyMs">The latency in milliseconds.</param>
public record ExecuteInferenceResult(
    Guid RequestId,
    string ResponseContent,
    TokenUsage TokenUsage,
    long LatencyMs);
