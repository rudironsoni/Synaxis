// <copyright file="CreateInferenceRequestCommand.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Application.Commands;

using MediatR;
using Microsoft.Extensions.AI;
using Synaxis.Inference.Domain.Aggregates;

/// <summary>
/// Command to create a new inference request.
/// </summary>
/// <param name="TenantId">The tenant identifier.</param>
/// <param name="UserId">The user identifier.</param>
/// <param name="ApiKeyId">The API key identifier.</param>
/// <param name="ModelId">The model identifier.</param>
/// <param name="Messages">The chat messages.</param>
/// <param name="Tools">The available tools.</param>
/// <param name="ToolChoice">The tool choice configuration.</param>
/// <param name="ResponseFormat">The response format configuration.</param>
/// <param name="Temperature">The sampling temperature.</param>
/// <param name="MaxTokens">The maximum tokens to generate.</param>
public record CreateInferenceRequestCommand(
    Guid TenantId,
    Guid? UserId,
    Guid? ApiKeyId,
    string ModelId,
    IReadOnlyList<ChatMessage> Messages,
    IList<AITool>? Tools = null,
    object? ToolChoice = null,
    object? ResponseFormat = null,
    double? Temperature = null,
    int? MaxTokens = null)
    : IRequest<CreateInferenceRequestResult>;

/// <summary>
/// Result of creating an inference request.
/// </summary>
/// <param name="RequestId">The unique request identifier.</param>
/// <param name="Status">The initial status of the request.</param>
public record CreateInferenceRequestResult(Guid RequestId, InferenceStatus Status);

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

/// <summary>
/// Command to execute an inference request.
/// </summary>
/// <param name="RequestId">The request identifier.</param>
public record ExecuteInferenceCommand(Guid RequestId)
    : IRequest<ExecuteInferenceResult>;

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

/// <summary>
/// Result of completing an inference request.
/// </summary>
/// <param name="RequestId">The request identifier.</param>
/// <param name="Status">The final status.</param>
public record CompleteInferenceResult(Guid RequestId, InferenceStatus Status);

/// <summary>
/// Command to fail an inference request.
/// </summary>
/// <param name="RequestId">The request identifier.</param>
/// <param name="ErrorMessage">The error message.</param>
public record FailInferenceCommand(Guid RequestId, string ErrorMessage)
    : IRequest<FailInferenceResult>;

/// <summary>
/// Result of failing an inference request.
/// </summary>
/// <param name="RequestId">The request identifier.</param>
/// <param name="Status">The final status.</param>
public record FailInferenceResult(Guid RequestId, InferenceStatus Status);

/// <summary>
/// Command to retry a failed inference request.
/// </summary>
/// <param name="RequestId">The request identifier.</param>
/// <param name="NewProviderId">The new provider to use.</param>
public record RetryInferenceCommand(Guid RequestId, string NewProviderId)
    : IRequest<RetryInferenceResult>;

/// <summary>
/// Result of retrying an inference request.
/// </summary>
/// <param name="RequestId">The request identifier.</param>
/// <param name="Status">The new status.</param>
public record RetryInferenceResult(Guid RequestId, InferenceStatus Status);
