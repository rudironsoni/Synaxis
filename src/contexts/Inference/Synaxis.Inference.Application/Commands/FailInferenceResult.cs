// <copyright file="FailInferenceResult.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Application.Commands;

/// <summary>
/// Result of failing an inference request.
/// </summary>
/// <param name="RequestId">The request identifier.</param>
/// <param name="Status">The final status.</param>
public record FailInferenceResult(Guid RequestId, InferenceStatus Status);
