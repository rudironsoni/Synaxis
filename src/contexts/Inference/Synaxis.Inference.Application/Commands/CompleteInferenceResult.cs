// <copyright file="CompleteInferenceResult.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Inference.Application.Commands;

using Synaxis.Inference.Domain.Entities;

/// <summary>
/// Result of completing an inference request.
/// </summary>
/// <param name="RequestId">The request identifier.</param>
/// <param name="Status">The final status.</param>
public record CompleteInferenceResult(Guid RequestId, InferenceStatus Status);
