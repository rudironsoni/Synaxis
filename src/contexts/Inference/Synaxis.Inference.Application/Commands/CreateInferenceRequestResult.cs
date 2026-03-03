// <copyright file="CreateInferenceRequestResult.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Application.Commands;

/// <summary>
/// Result of creating an inference request.
/// </summary>
/// <param name="RequestId">The unique request identifier.</param>
/// <param name="Status">The initial status of the request.</param>
public record CreateInferenceRequestResult(Guid RequestId, InferenceStatus Status);
