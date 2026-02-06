// <copyright file="SandboxTestResult.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.ControlPlane.Entities;

/// <summary>
/// Results from sandbox testing a provider before production activation.
/// </summary>
public record SandboxTestResult(
    bool successful,
    string? testModel,
    string? errorMessage);
