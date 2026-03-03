// <copyright file="RetryInferenceResult.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Application.Commands;

/// <summary>
/// Result of retrying an inference request.
/// </summary>
/// <param name="RequestId">The request identifier.</param>
/// <param name="Status">The new status.</param>
public record RetryInferenceResult(Guid RequestId, InferenceStatus Status);
